namespace TicTacToe.Api.Domain;

/// <summary>
/// Deterministic rule-based opponent. Priority:
/// 1. Win  2. Block  3. Center  4. Corner  5. Any empty cell.
/// </summary>
public static class ComputerStrategy
{
    private static readonly Position Center = new(1, 1);

    private static readonly Position[] Corners = [new(0, 0), new(0, 2), new(2, 0), new(2, 2)];

    public static Position ChooseMove(Board board, Player computer)
    {
        var empty = board.EmptyPositions.ToList();
        if (empty.Count == 0)
        {
            throw new InvalidOperationException("The board is full; there is no move to make.");
        }

        return FindCompletingMove(board, computer)
            ?? FindCompletingMove(board, computer.Opponent())
            ?? (board.IsEmpty(Center) ? Center : (Position?)null)
            ?? Corners.Where(board.IsEmpty).Cast<Position?>().FirstOrDefault()
            ?? empty[0];
    }

    /// <summary>Finds an empty cell that would complete a line of three for <paramref name="player"/>.</summary>
    private static Position? FindCompletingMove(Board board, Player player)
    {
        foreach (var line in WinDetector.Lines)
        {
            var owned = line.Count(position => board[position] == player);
            var empties = line.Where(board.IsEmpty).ToArray();
            if (owned == Board.Size - 1 && empties.Length == 1)
            {
                return empties[0];
            }
        }

        return null;
    }
}
