namespace TicTacToe.Api.Domain;

/// <summary>
/// A single Tic Tac Toe game. The move list is the source of truth; board, turn and
/// status are always derived from it, which keeps Undo trivially correct.
/// </summary>
public sealed class Game
{
    public const Player HumanPlayer = Player.X;
    public const Player ComputerPlayer = Player.O;

    private readonly List<Move> _moves = [];

    public Game(Guid id, GameMode mode)
    {
        Id = id;
        Mode = mode;
    }

    public Guid Id { get; }

    public GameMode Mode { get; }

    public Board Board { get; private set; } = new();

    public IReadOnlyList<Move> Moves => _moves;

    public Player CurrentPlayer { get; private set; } = Player.X;

    public GameStatus Status { get; private set; } = GameStatus.InProgress;

    public Player? Winner { get; private set; }

    public IReadOnlyList<Position> WinningCells { get; private set; } = [];

    public bool IsCompleted => Status != GameStatus.InProgress;

    public bool CanUndo => _moves.Count > 0;

    public Move Play(Player player, Position position)
    {
        if (IsCompleted)
        {
            throw new GameRuleException(GameErrorCode.GameCompleted, "The game is already completed. Reset to start a new game.");
        }

        if (!position.IsOnBoard)
        {
            throw new GameRuleException(
                GameErrorCode.OutOfBounds,
                $"Position ({position.Row}, {position.Column}) is outside the board. Row and column must be between 0 and {Board.Size - 1}.");
        }

        if (player != CurrentPlayer)
        {
            throw new GameRuleException(GameErrorCode.WrongPlayer, $"It is {CurrentPlayer}'s turn, not {player}'s.");
        }

        if (!Board.IsEmpty(position))
        {
            throw new GameRuleException(GameErrorCode.CellOccupied, $"Cell {position.Label} is already occupied.");
        }

        var move = new Move(_moves.Count + 1, player, position);
        Board.Place(player, position);
        _moves.Add(move);
        UpdateDerivedState();
        return move;
    }

    /// <summary>
    /// Two Player: removes the last move.
    /// Computer: removes the computer's last move together with the human move before it,
    /// so it is always the human's (X's) turn afterwards. If the human made the last move
    /// (e.g. a winning or board-filling move), only that move is removed.
    /// </summary>
    public IReadOnlyList<Move> Undo()
    {
        if (_moves.Count == 0)
        {
            throw new GameRuleException(GameErrorCode.NothingToUndo, "There are no moves to undo.");
        }

        var removeCount = Mode == GameMode.Computer && _moves[^1].Player == ComputerPlayer && _moves.Count >= 2 ? 2 : 1;
        var removed = _moves.GetRange(_moves.Count - removeCount, removeCount);
        _moves.RemoveRange(_moves.Count - removeCount, removeCount);
        Rebuild();
        return removed;
    }

    public void Reset()
    {
        _moves.Clear();
        Rebuild();
    }

    private void Rebuild()
    {
        Board = new Board();
        foreach (var move in _moves)
        {
            Board.Place(move.Player, move.Position);
        }

        UpdateDerivedState();
    }

    private void UpdateDerivedState()
    {
        var win = WinDetector.FindWinner(Board);
        if (win is not null)
        {
            Status = GameStatus.Won;
            Winner = win.Winner;
            WinningCells = win.Cells;
        }
        else
        {
            Status = Board.IsFull ? GameStatus.Draw : GameStatus.InProgress;
            Winner = null;
            WinningCells = [];
        }

        CurrentPlayer = _moves.Count == 0 ? Player.X : _moves[^1].Player.Opponent();
    }
}
