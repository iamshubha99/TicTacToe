using Microsoft.AspNetCore.Http.HttpResults;
using TicTacToe.Api.Application;
using TicTacToe.Api.Contracts;
using TicTacToe.Api.Domain;

namespace TicTacToe.Api.Endpoints;

public static class GameEndpoints
{
    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var games = app.MapGroup("/api/games").WithTags("Games");

        games.MapPost("/", (CreateGameRequest? request, GameService service) =>
        {
            var state = service.Create(request?.Mode ?? GameMode.TwoPlayer);
            return TypedResults.Created($"/api/games/{state.Id}", state);
        }).WithSummary("Create a new game session");

        games.MapGet("/{id:guid}", (Guid id, GameService service) => TypedResults.Ok(service.Get(id)))
            .WithSummary("Get current game state");

        games.MapPost("/{id:guid}/moves", Ok<GameStateResponse> (Guid id, MoveRequest request, GameService service) =>
        {
            if (request.GameId is { } bodyId && bodyId != id)
            {
                throw new GameRuleException(GameErrorCode.InvalidRequest, "gameId in the body does not match the game in the URL.");
            }

            if (request.Player is not { } player || request.Row is not { } row || request.Column is not { } column)
            {
                throw new GameRuleException(GameErrorCode.InvalidRequest, "player, row and column are required.");
            }

            return TypedResults.Ok(service.Play(id, player, row, column));
        }).WithSummary("Submit a player move (in Computer mode the computer replies in the same response)");

        games.MapPost("/{id:guid}/undo", (Guid id, GameService service) => TypedResults.Ok(service.Undo(id)))
            .WithSummary("Undo the last move (Two Player) or the last move pair (Computer)");

        games.MapPost("/{id:guid}/reset", (Guid id, GameService service) => TypedResults.Ok(service.Reset(id)))
            .WithSummary("Reset the current game; the scoreboard is kept");

        var scoreboard = app.MapGroup("/api/scoreboard").WithTags("Scoreboard");

        scoreboard.MapGet("/", (GameService service) => TypedResults.Ok(service.GetScoreboard()))
            .WithSummary("Get scoreboard");

        scoreboard.MapPost("/reset", (GameService service) => TypedResults.Ok(service.ResetScoreboard()))
            .WithSummary("Reset scoreboard");

        return app;
    }
}
