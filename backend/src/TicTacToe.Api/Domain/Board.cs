namespace TicTacToe.Api.Domain;

public sealed class Board
{
    public const int Size = 3;
    public const int CellCount = Size * Size;

    private readonly Player?[] _cells = new Player?[CellCount];

    public Player? this[Position position] => _cells[position.Index];

    public bool IsEmpty(Position position) => _cells[position.Index] is null;

    public bool IsFull => _cells.All(cell => cell is not null);

    public IEnumerable<Position> EmptyPositions =>
        Enumerable.Range(0, CellCount).Where(i => _cells[i] is null).Select(Position.FromIndex);

    public void Place(Player player, Position position)
    {
        if (!position.IsOnBoard)
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "Position is outside the board.");
        }

        if (!IsEmpty(position))
        {
            throw new InvalidOperationException($"Cell {position.Label} is already occupied.");
        }

        _cells[position.Index] = player;
    }

    public Player?[][] ToRows() =>
        Enumerable.Range(0, Size)
            .Select(row => Enumerable.Range(0, Size).Select(col => _cells[row * Size + col]).ToArray())
            .ToArray();
}
