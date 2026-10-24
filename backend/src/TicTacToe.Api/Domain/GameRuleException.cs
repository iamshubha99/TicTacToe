namespace TicTacToe.Api.Domain;

public enum GameErrorCode
{
    InvalidRequest,
    OutOfBounds,
    CellOccupied,
    WrongPlayer,
    GameCompleted,
    NothingToUndo,
}

/// <summary>Raised when an action violates the game rules. The game state is left unchanged.</summary>
public sealed class GameRuleException(GameErrorCode code, string message) : Exception(message)
{
    public GameErrorCode Code { get; } = code;
}

public sealed class GameNotFoundException(Guid id) : Exception($"Game '{id}' was not found.")
{
    public Guid GameId { get; } = id;
}
