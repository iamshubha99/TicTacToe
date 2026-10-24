namespace TicTacToe.Api.Domain;

public sealed record WinResult(Player Winner, IReadOnlyList<Position> Cells);

public static class WinDetector
{
    /// <summary>All 8 winning lines: 3 rows, 3 columns, 2 diagonals.</summary>
    public static readonly IReadOnlyList<Position[]> Lines = BuildLines();

    public static WinResult? FindWinner(Board board)
    {
        foreach (var line in Lines)
        {
            var first = board[line[0]];
            if (first is not null && line.All(position => board[position] == first))
            {
                return new WinResult(first.Value, line);
            }
        }

        return null;
    }

    private static Position[][] BuildLines()
    {
        var lines = new List<Position[]>();
        for (var i = 0; i < Board.Size; i++)
        {
            lines.Add(Enumerable.Range(0, Board.Size).Select(col => new Position(i, col)).ToArray());
            lines.Add(Enumerable.Range(0, Board.Size).Select(row => new Position(row, i)).ToArray());
        }

        lines.Add(Enumerable.Range(0, Board.Size).Select(i => new Position(i, i)).ToArray());
        lines.Add(Enumerable.Range(0, Board.Size).Select(i => new Position(i, Board.Size - 1 - i)).ToArray());
        return lines.ToArray();
    }
}
