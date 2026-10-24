using TicTacToe.Api.Domain;

namespace TicTacToe.Tests;

public class GameTests
{
    [Fact]
    public void NewGame_HasEmptyBoard_AndXToMove()
    {
        var game = TestHelpers.NewGame();

        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Empty(game.Moves);
        Assert.Equal(9, game.Board.EmptyPositions.Count());
        Assert.False(game.CanUndo);
    }

    [Fact]
    public void Play_ValidMove_PlacesMarkAndRecordsHistory()
    {
        var game = TestHelpers.NewGame();

        var move = game.Play(Player.X, new Position(0, 0));

        Assert.Equal(new Move(1, Player.X, new Position(0, 0)), move);
        Assert.Equal(Player.X, game.Board[new Position(0, 0)]);
        Assert.Single(game.Moves);
        Assert.Equal("Row 1, Column 1", game.Moves[0].Position.Label);
        Assert.True(game.CanUndo);
    }

    [Fact]
    public void Play_AlternatesTurns()
    {
        var game = TestHelpers.NewGame();

        game.Play(Player.X, new Position(0, 0));
        Assert.Equal(Player.O, game.CurrentPlayer);

        game.Play(Player.O, new Position(1, 1));
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal([1, 2], game.Moves.Select(m => m.Number));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(3, 0)]
    [InlineData(0, 3)]
    public void Play_OutsideBoard_IsRejected_AndTurnUnchanged(int row, int column)
    {
        var game = TestHelpers.NewGame();

        var ex = Assert.Throws<GameRuleException>(() => game.Play(Player.X, new Position(row, column)));

        Assert.Equal(GameErrorCode.OutOfBounds, ex.Code);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Empty(game.Moves);
    }

    [Fact]
    public void Play_OnOccupiedCell_IsRejected_AndTurnUnchanged()
    {
        var game = TestHelpers.NewGame().PlayCells(4);

        var ex = Assert.Throws<GameRuleException>(() => game.Play(Player.O, new Position(1, 1)));

        Assert.Equal(GameErrorCode.CellOccupied, ex.Code);
        Assert.Equal(Player.O, game.CurrentPlayer);
        Assert.Single(game.Moves);
    }

    [Fact]
    public void Play_ByWrongPlayer_IsRejected()
    {
        var game = TestHelpers.NewGame();

        var ex = Assert.Throws<GameRuleException>(() => game.Play(Player.O, new Position(0, 0)));

        Assert.Equal(GameErrorCode.WrongPlayer, ex.Code);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Empty(game.Moves);
    }

    [Theory]
    [InlineData(0, new[] { 0, 3, 1, 4, 2 })]
    [InlineData(1, new[] { 3, 0, 4, 1, 5 })]
    [InlineData(2, new[] { 6, 0, 7, 1, 8 })]
    public void RowWin_IsDetected_WithWinningCells(int row, int[] cells)
    {
        var game = TestHelpers.NewGame().PlayCells(cells);

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.X, game.Winner);
        Assert.Equal([new(row, 0), new(row, 1), new(row, 2)], game.WinningCells);
    }

    [Theory]
    [InlineData(0, new[] { 0, 1, 3, 4, 6 })]
    [InlineData(1, new[] { 1, 0, 4, 3, 7 })]
    [InlineData(2, new[] { 2, 0, 5, 3, 8 })]
    public void ColumnWin_IsDetected_WithWinningCells(int column, int[] cells)
    {
        var game = TestHelpers.NewGame().PlayCells(cells);

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.X, game.Winner);
        Assert.Equal([new(0, column), new(1, column), new(2, column)], game.WinningCells);
    }

    [Fact]
    public void MainDiagonalWin_IsDetected()
    {
        var game = TestHelpers.NewGame().PlayCells(0, 1, 4, 2, 8);

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.X, game.Winner);
        Assert.Equal([new(0, 0), new(1, 1), new(2, 2)], game.WinningCells);
    }

    [Fact]
    public void AntiDiagonalWin_ForO_IsDetected()
    {
        // X: 0,1,3  O: 2,4,6
        var game = TestHelpers.NewGame().PlayCells(0, 2, 1, 4, 3, 6);

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.O, game.Winner);
        Assert.Equal([new(0, 2), new(1, 1), new(2, 0)], game.WinningCells);
    }

    [Fact]
    public void FullBoardWithoutWinner_IsDraw()
    {
        // X O X
        // X O O
        // O X X
        var game = TestHelpers.NewGame().PlayCells(0, 1, 2, 4, 3, 5, 7, 6, 8);

        Assert.Equal(GameStatus.Draw, game.Status);
        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
        Assert.Equal(9, game.Moves.Count);
    }

    [Fact]
    public void WinOnLastCell_IsWinNotDraw()
    {
        // Final board, X completes the main diagonal with the 9th move (cell 0):
        // X O X
        // O X O
        // O X X
        var game = TestHelpers.NewGame().PlayCells(4, 1, 2, 6, 7, 3, 8, 5, 0);

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.X, game.Winner);
        Assert.Equal([new(0, 0), new(1, 1), new(2, 2)], game.WinningCells);
    }

    [Fact]
    public void Play_AfterWin_IsRejected()
    {
        var game = TestHelpers.NewGame().PlayCells(0, 3, 1, 4, 2);

        var ex = Assert.Throws<GameRuleException>(() => game.Play(Player.O, new Position(2, 2)));

        Assert.Equal(GameErrorCode.GameCompleted, ex.Code);
        Assert.Equal(5, game.Moves.Count);
    }

    [Fact]
    public void Play_AfterDraw_IsRejected()
    {
        var game = TestHelpers.NewGame().PlayCells(0, 1, 2, 4, 3, 5, 7, 6, 8);

        var ex = Assert.Throws<GameRuleException>(() => game.Play(Player.O, new Position(0, 0)));

        Assert.Equal(GameErrorCode.GameCompleted, ex.Code);
    }

    [Fact]
    public void Reset_ClearsBoardHistoryAndStatus_AndXStarts()
    {
        var game = TestHelpers.NewGame(GameMode.Computer).PlayCells(0, 3, 1, 4, 2);

        game.Reset();

        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
        Assert.Empty(game.Moves);
        Assert.Equal(9, game.Board.EmptyPositions.Count());
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(GameMode.Computer, game.Mode);
    }

    [Fact]
    public void Undo_TwoPlayer_RemovesOnlyLastMove()
    {
        var game = TestHelpers.NewGame().PlayCells(0, 4);

        var removed = game.Undo();

        Assert.Single(removed);
        Assert.Equal(Player.O, removed[0].Player);
        Assert.Equal(Player.O, game.CurrentPlayer);
        Assert.Single(game.Moves);
        Assert.Null(game.Board[new Position(1, 1)]);
        Assert.Equal(Player.X, game.Board[new Position(0, 0)]);
    }

    [Fact]
    public void Undo_ComputerMode_RemovesComputerAndHumanMoveTogether()
    {
        var game = TestHelpers.NewGame(GameMode.Computer).PlayCells(0, 4, 8, 1);

        var removed = game.Undo();

        Assert.Equal([Player.X, Player.O], removed.Select(m => m.Player));
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(2, game.Moves.Count);
        Assert.Null(game.Board[new Position(2, 2)]);
        Assert.Null(game.Board[new Position(0, 1)]);
    }

    [Fact]
    public void Undo_ComputerMode_WhenHumanMadeLastMove_RemovesOnlyThatMove()
    {
        // Human X wins on move 5; there is no computer reply to remove.
        var game = TestHelpers.NewGame(GameMode.Computer).PlayCells(0, 3, 1, 4, 2);

        var removed = game.Undo();

        Assert.Single(removed);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(4, game.Moves.Count);
    }

    [Fact]
    public void Undo_AfterWin_RecalculatesStatus()
    {
        var game = TestHelpers.NewGame().PlayCells(0, 3, 1, 4, 2);

        game.Undo();

        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
        Assert.Equal(Player.X, game.CurrentPlayer);
    }

    [Fact]
    public void Undo_AfterDraw_RecalculatesStatus()
    {
        var game = TestHelpers.NewGame().PlayCells(0, 1, 2, 4, 3, 5, 7, 6, 8);

        game.Undo();

        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(Player.X, game.CurrentPlayer);
    }

    [Fact]
    public void Undo_WithNoMoves_IsRejected()
    {
        var game = TestHelpers.NewGame();

        var ex = Assert.Throws<GameRuleException>(() => game.Undo());

        Assert.Equal(GameErrorCode.NothingToUndo, ex.Code);
    }

    [Fact]
    public void Undo_KeepsMoveNumbersSequential()
    {
        var game = TestHelpers.NewGame().PlayCells(0, 4, 8);
        game.Undo();
        game.PlayCells(2);

        Assert.Equal([1, 2, 3], game.Moves.Select(m => m.Number));
        Assert.Equal(new Position(0, 2), game.Moves[^1].Position);
    }
}
