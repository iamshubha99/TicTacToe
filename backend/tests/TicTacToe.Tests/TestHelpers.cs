using TicTacToe.Api.Domain;

namespace TicTacToe.Tests;

internal static class TestHelpers
{
    /// <summary>Plays the given cells (0..8, row-major) alternating from the current player.</summary>
    public static Game PlayCells(this Game game, params int[] cells)
    {
        foreach (var cell in cells)
        {
            game.Play(game.CurrentPlayer, Position.FromIndex(cell));
        }

        return game;
    }

    public static Game NewGame(GameMode mode = GameMode.TwoPlayer) => new(Guid.NewGuid(), mode);

    /// <summary>Builds a board from a 9-char layout like "XO.X....." ('.' = empty).</summary>
    public static Board BoardFrom(string layout)
    {
        Assert.Equal(Board.CellCount, layout.Length);
        var board = new Board();
        for (var i = 0; i < layout.Length; i++)
        {
            switch (layout[i])
            {
                case 'X': board.Place(Player.X, Position.FromIndex(i)); break;
                case 'O': board.Place(Player.O, Position.FromIndex(i)); break;
            }
        }

        return board;
    }
}
