namespace TicTacToe.Api.Domain;

public enum Player
{
    X,
    O,
}

public enum GameMode
{
    TwoPlayer,
    Computer,
}

public enum GameStatus
{
    InProgress,
    Won,
    Draw,
}

public static class PlayerExtensions
{
    public static Player Opponent(this Player player) => player == Player.X ? Player.O : Player.X;
}

/// <summary>Zero-based board coordinate.</summary>
public readonly record struct Position(int Row, int Column)
{
    public bool IsOnBoard => Row is >= 0 and < Board.Size && Column is >= 0 and < Board.Size;

    public int Index => Row * Board.Size + Column;

    public static Position FromIndex(int index) => new(index / Board.Size, index % Board.Size);

    /// <summary>Human-friendly, one-based label, e.g. "Row 1, Column 1".</summary>
    public string Label => $"Row {Row + 1}, Column {Column + 1}";
}

public sealed record Move(int Number, Player Player, Position Position);
