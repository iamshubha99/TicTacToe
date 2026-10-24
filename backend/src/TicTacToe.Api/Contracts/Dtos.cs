using TicTacToe.Api.Domain;

namespace TicTacToe.Api.Contracts;

public sealed record CreateGameRequest(GameMode Mode = GameMode.TwoPlayer);

/// <summary>Row and column are zero-based (0..2). <see cref="GameId"/> is optional; if supplied it must match the route.</summary>
public sealed record MoveRequest(Guid? GameId, Player? Player, int? Row, int? Column);

public sealed record PositionDto(int Row, int Column);

public sealed record MoveDto(int Number, Player Player, int Row, int Column, string Position);

public sealed record ScoreboardDto(int XWins, int OWins, int Draws);

public sealed record GameStateResponse(
    Guid Id,
    GameMode Mode,
    Player?[][] Board,
    Player? CurrentPlayer,
    GameStatus Status,
    Player? Winner,
    IReadOnlyList<PositionDto> WinningCells,
    IReadOnlyList<MoveDto> Moves,
    bool CanUndo,
    ScoreboardDto Scoreboard);

public static class ContractMapping
{
    public static GameStateResponse ToResponse(this Game game, ScoreSnapshot score) => new(
        game.Id,
        game.Mode,
        game.Board.ToRows(),
        game.IsCompleted ? null : game.CurrentPlayer,
        game.Status,
        game.Winner,
        game.WinningCells.Select(p => new PositionDto(p.Row, p.Column)).ToList(),
        game.Moves.Select(m => new MoveDto(m.Number, m.Player, m.Position.Row, m.Position.Column, m.Position.Label)).ToList(),
        game.CanUndo,
        score.ToDto());

    public static ScoreboardDto ToDto(this ScoreSnapshot score) => new(score.XWins, score.OWins, score.Draws);
}
