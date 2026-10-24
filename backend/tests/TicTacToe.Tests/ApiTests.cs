using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using TicTacToe.Api.Contracts;
using TicTacToe.Api.Domain;

namespace TicTacToe.Tests;

/// <summary>End-to-end tests through the HTTP pipeline (routing, JSON, error mapping).</summary>
public class ApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly WebApplicationFactory<Program> _factory = new();
    private readonly HttpClient _client;

    public ApiTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<GameStateResponse> CreateGame(string mode = "TwoPlayer")
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { mode });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        return (await response.Content.ReadFromJsonAsync<GameStateResponse>(Json))!;
    }

    private Task<HttpResponseMessage> Move(Guid id, string player, int row, int column) =>
        _client.PostAsJsonAsync($"/api/games/{id}/moves", new { gameId = id, player, row, column });

    [Fact]
    public async Task CreateGame_ReturnsInitialState_WithStringEnums()
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { mode = "Computer" });
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("\"mode\":\"Computer\"", json);
        Assert.Contains("\"status\":\"InProgress\"", json);
        Assert.Contains("\"currentPlayer\":\"X\"", json);
    }

    [Fact]
    public async Task CreateGame_WithoutBody_DefaultsToTwoPlayer()
    {
        var response = await _client.PostAsync("/api/games", null);
        var state = await response.Content.ReadFromJsonAsync<GameStateResponse>(Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(GameMode.TwoPlayer, state!.Mode);
    }

    [Fact]
    public async Task GetGame_ReturnsState()
    {
        var created = await CreateGame();

        var state = await _client.GetFromJsonAsync<GameStateResponse>($"/api/games/{created.Id}", Json);

        Assert.Equal(created.Id, state!.Id);
    }

    [Fact]
    public async Task GetGame_Unknown_Returns404Problem()
    {
        var response = await _client.GetAsync($"/api/games/{Guid.NewGuid()}");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("GameNotFound", problem!.Extensions["code"]!.ToString());
    }

    [Fact]
    public async Task ValidMove_Returns200_WithUpdatedState()
    {
        var game = await CreateGame();

        var response = await Move(game.Id, "X", 1, 1);
        var state = await response.Content.ReadFromJsonAsync<GameStateResponse>(Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Player.X, state!.Board[1][1]);
        Assert.Equal(Player.O, state.CurrentPlayer);
        Assert.Equal("Row 2, Column 2", state.Moves.Single().Position);
    }

    [Theory]
    [InlineData("X", 3, 0, HttpStatusCode.BadRequest, "OutOfBounds")]
    [InlineData("O", 0, 0, HttpStatusCode.Conflict, "WrongPlayer")]
    public async Task InvalidMove_ReturnsProblemWithCode(string player, int row, int column, HttpStatusCode status, string code)
    {
        var game = await CreateGame();

        var response = await Move(game.Id, player, row, column);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, problem!.Extensions["code"]!.ToString());
    }

    [Fact]
    public async Task OccupiedCell_Returns409_AndStateUnchanged()
    {
        var game = await CreateGame();
        await Move(game.Id, "X", 0, 0);

        var response = await Move(game.Id, "O", 0, 0);
        var state = await _client.GetFromJsonAsync<GameStateResponse>($"/api/games/{game.Id}", Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Single(state!.Moves);
        Assert.Equal(Player.O, state.CurrentPlayer);
    }

    [Fact]
    public async Task MissingFields_Returns400()
    {
        var game = await CreateGame();

        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/moves", new { player = "X" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MismatchedGameId_Returns400()
    {
        var game = await CreateGame();

        var response = await _client.PostAsJsonAsync(
            $"/api/games/{game.Id}/moves", new { gameId = Guid.NewGuid(), player = "X", row = 0, column = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FullFlow_Win_Undo_Reset_And_Scoreboard()
    {
        await _client.PostAsync("/api/scoreboard/reset", null);
        var game = await CreateGame();

        foreach (var (player, row, column) in new[] { ("X", 0, 0), ("O", 1, 0), ("X", 0, 1), ("O", 1, 1), ("X", 0, 2) })
        {
            (await Move(game.Id, player, row, column)).EnsureSuccessStatusCode();
        }

        var won = await _client.GetFromJsonAsync<GameStateResponse>($"/api/games/{game.Id}", Json);
        Assert.Equal(GameStatus.Won, won!.Status);
        Assert.Equal(3, won.WinningCells.Count);
        Assert.Equal(new ScoreboardDto(1, 0, 0), await _client.GetFromJsonAsync<ScoreboardDto>("/api/scoreboard", Json));

        var afterWinMove = await Move(game.Id, "O", 2, 2);
        Assert.Equal(HttpStatusCode.Conflict, afterWinMove.StatusCode);

        var undoResponse = await _client.PostAsync($"/api/games/{game.Id}/undo", null);
        var undone = await undoResponse.Content.ReadFromJsonAsync<GameStateResponse>(Json);
        Assert.Equal(GameStatus.InProgress, undone!.Status);
        Assert.Equal(new ScoreboardDto(0, 0, 0), undone.Scoreboard);

        var resetResponse = await _client.PostAsync($"/api/games/{game.Id}/reset", null);
        var reset = await resetResponse.Content.ReadFromJsonAsync<GameStateResponse>(Json);
        Assert.Empty(reset!.Moves);

        var emptyUndo = await _client.PostAsync($"/api/games/{game.Id}/undo", null);
        Assert.Equal(HttpStatusCode.Conflict, emptyUndo.StatusCode);
    }

    [Fact]
    public async Task ComputerMode_MoveReturnsComputerReply()
    {
        var game = await CreateGame("Computer");

        var response = await Move(game.Id, "X", 0, 0);
        var state = await response.Content.ReadFromJsonAsync<GameStateResponse>(Json);

        Assert.Equal(2, state!.Moves.Count);
        Assert.Equal(Player.O, state.Board[1][1]);
        Assert.Equal(Player.X, state.CurrentPlayer);
    }

    [Fact]
    public async Task Cors_AllowsAngularDevServer()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/scoreboard");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await _client.SendAsync(request);

        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
