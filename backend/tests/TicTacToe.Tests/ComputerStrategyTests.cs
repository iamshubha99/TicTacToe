using TicTacToe.Api.Domain;

namespace TicTacToe.Tests;

public class ComputerStrategyTests
{
    [Fact]
    public void TakesWinningMove()
    {
        // O O . / X X . / X . .
        var board = TestHelpers.BoardFrom("OO.XX.X..");

        Assert.Equal(new Position(0, 2), ComputerStrategy.ChooseMove(board, Player.O));
    }

    [Fact]
    public void PrefersWinningOverBlocking()
    {
        // X X . / O O . / X . .  -> O can win at (1,2); X threatens (0,2)
        var board = TestHelpers.BoardFrom("XX.OO.X..");

        Assert.Equal(new Position(1, 2), ComputerStrategy.ChooseMove(board, Player.O));
    }

    [Fact]
    public void BlocksOpponentWin()
    {
        // X X . / . O . / . . .
        var board = TestHelpers.BoardFrom("XX..O....");

        Assert.Equal(new Position(0, 2), ComputerStrategy.ChooseMove(board, Player.O));
    }

    [Fact]
    public void TakesCenterWhenAvailable()
    {
        var board = TestHelpers.BoardFrom("X........");

        Assert.Equal(new Position(1, 1), ComputerStrategy.ChooseMove(board, Player.O));
    }

    [Fact]
    public void TakesCornerWhenCenterTaken()
    {
        var board = TestHelpers.BoardFrom("....X....");

        Assert.Equal(new Position(0, 0), ComputerStrategy.ChooseMove(board, Player.O));
    }

    [Fact]
    public void TakesAnyCellWhenCenterAndCornersTaken()
    {
        // X O X / . X . / O X O  -> no win or block available; center and all corners are taken
        var board = TestHelpers.BoardFrom("XOX.X.OXO");

        var move = ComputerStrategy.ChooseMove(board, Player.O);

        Assert.Contains(move, new[] { new Position(1, 0), new Position(1, 2) });
        Assert.True(board.IsEmpty(move));
    }

    [Fact]
    public void AlwaysChoosesAnEmptyCell()
    {
        var random = new Random(42);
        for (var i = 0; i < 500; i++)
        {
            var game = TestHelpers.NewGame();
            while (!game.IsCompleted)
            {
                var empty = game.Board.EmptyPositions.ToList();
                var position = game.CurrentPlayer == Player.O
                    ? ComputerStrategy.ChooseMove(game.Board, Player.O)
                    : empty[random.Next(empty.Count)];

                Assert.True(game.Board.IsEmpty(position));
                game.Play(game.CurrentPlayer, position);
            }
        }
    }

    [Fact]
    public void ThrowsWhenBoardIsFull()
    {
        var board = TestHelpers.BoardFrom("XOXXOOOXX");

        Assert.Throws<InvalidOperationException>(() => ComputerStrategy.ChooseMove(board, Player.O));
    }
}
