using TicTacToe.Api.Application;
using TicTacToe.Api.Contracts;
using TicTacToe.Api.Domain;

namespace TicTacToe.Tests;

public class GameServiceTests
{
    private readonly GameService _service = new(new InMemoryGameSessionStore(), new Scoreboard());

    private GameStateResponse PlayCells(Guid id, params int[] cells)
    {
        GameStateResponse state = _service.Get(id);
        foreach (var cell in cells)
        {
            var position = Position.FromIndex(cell);
            state = _service.Play(id, state.CurrentPlayer!.Value, position.Row, position.Column);
        }

        return state;
    }

    [Fact]
    public void Create_ReturnsFreshGameStateWithScoreboard()
    {
        var state = _service.Create(GameMode.Computer);

        Assert.NotEqual(Guid.Empty, state.Id);
        Assert.Equal(GameMode.Computer, state.Mode);
        Assert.Equal(GameStatus.InProgress, state.Status);
        Assert.Equal(Player.X, state.CurrentPlayer);
        Assert.All(state.Board.SelectMany(r => r), cell => Assert.Null(cell));
        Assert.Equal(new ScoreboardDto(0, 0, 0), state.Scoreboard);
        Assert.False(state.CanUndo);
    }

    [Fact]
    public void Get_UnknownGame_Throws()
    {
        Assert.Throws<GameNotFoundException>(() => _service.Get(Guid.NewGuid()));
    }

    [Fact]
    public void Win_UpdatesScoreboardExactlyOnce()
    {
        var id = _service.Create(GameMode.TwoPlayer).Id;

        var state = PlayCells(id, 0, 3, 1, 4, 2);

        Assert.Equal(GameStatus.Won, state.Status);
        Assert.Equal(Player.X, state.Winner);
        Assert.Null(state.CurrentPlayer);
        Assert.Equal(new ScoreboardDto(1, 0, 0), state.Scoreboard);

        var ex = Assert.Throws<GameRuleException>(() => _service.Play(id, Player.O, 2, 2));
        Assert.Equal(GameErrorCode.GameCompleted, ex.Code);
        Assert.Equal(new ScoreboardDto(1, 0, 0), _service.Get(id).Scoreboard);
        Assert.Equal(new ScoreboardDto(1, 0, 0), _service.GetScoreboard());
    }

    [Fact]
    public void Draw_UpdatesScoreboard()
    {
        var id = _service.Create(GameMode.TwoPlayer).Id;

        var state = PlayCells(id, 0, 1, 2, 4, 3, 5, 7, 6, 8);

        Assert.Equal(GameStatus.Draw, state.Status);
        Assert.Equal(new ScoreboardDto(0, 0, 1), state.Scoreboard);
    }

    [Fact]
    public void ComputerMode_ComputerRepliesAutomatically()
    {
        var id = _service.Create(GameMode.Computer).Id;

        var state = _service.Play(id, Player.X, 0, 0);

        Assert.Equal(2, state.Moves.Count);
        Assert.Equal(Player.O, state.Moves[1].Player);
        Assert.Equal((1, 1), (state.Moves[1].Row, state.Moves[1].Column));
        Assert.Equal(Player.X, state.CurrentPlayer);
    }

    [Fact]
    public void ComputerMode_HumanCannotPlayAsO()
    {
        var id = _service.Create(GameMode.Computer).Id;

        var ex = Assert.Throws<GameRuleException>(() => _service.Play(id, Player.O, 0, 0));

        Assert.Equal(GameErrorCode.WrongPlayer, ex.Code);
    }

    [Fact]
    public void ComputerMode_ComputerWinsAndScoreboardUpdates()
    {
        var id = _service.Create(GameMode.Computer).Id;

        // X(0,0) -> O center; X(0,1) -> O blocks (0,2); X(2,2) -> O wins anti-diagonal (2,0).
        _service.Play(id, Player.X, 0, 0);
        _service.Play(id, Player.X, 0, 1);
        var state = _service.Play(id, Player.X, 2, 2);

        Assert.Equal(GameStatus.Won, state.Status);
        Assert.Equal(Player.O, state.Winner);
        Assert.Equal([new PositionDto(0, 2), new PositionDto(1, 1), new PositionDto(2, 0)], state.WinningCells);
        Assert.Equal(new ScoreboardDto(0, 1, 0), state.Scoreboard);
    }

    [Fact]
    public void ComputerMode_ComputerDoesNotMoveAfterHumanCompletesGame()
    {
        var id = _service.Create(GameMode.Computer).Id;

        // X(0,0) -> O center; X(2,2) -> O corner (0,2); X(2,0) blocks and forks -> O blocks (1,0); X(2,1) wins row 3.
        _service.Play(id, Player.X, 0, 0);
        _service.Play(id, Player.X, 2, 2);
        _service.Play(id, Player.X, 2, 0);
        var state = _service.Play(id, Player.X, 2, 1);

        Assert.Equal(GameStatus.Won, state.Status);
        Assert.Equal(Player.X, state.Winner);
        Assert.Equal(7, state.Moves.Count);
        Assert.Equal(Player.X, state.Moves[^1].Player);
        Assert.Equal(new ScoreboardDto(1, 0, 0), state.Scoreboard);

        // Undo in computer mode after the human's winning move removes just that move.
        var undone = _service.Undo(id);
        Assert.Equal(6, undone.Moves.Count);
        Assert.Equal(Player.X, undone.CurrentPlayer);
        Assert.Equal(new ScoreboardDto(0, 0, 0), undone.Scoreboard);
    }

    [Fact]
    public void Undo_TwoPlayer_RemovesLastMove()
    {
        var id = _service.Create(GameMode.TwoPlayer).Id;
        PlayCells(id, 0, 4);

        var state = _service.Undo(id);

        Assert.Single(state.Moves);
        Assert.Equal(Player.O, state.CurrentPlayer);
        Assert.Null(state.Board[1][1]);
    }

    [Fact]
    public void Undo_ComputerMode_RemovesMovePair()
    {
        var id = _service.Create(GameMode.Computer).Id;
        _service.Play(id, Player.X, 0, 0);

        var state = _service.Undo(id);

        Assert.Empty(state.Moves);
        Assert.Equal(Player.X, state.CurrentPlayer);
        Assert.False(state.CanUndo);
    }

    [Fact]
    public void Undo_WithNoMoves_IsRejected()
    {
        var id = _service.Create(GameMode.TwoPlayer).Id;

        var ex = Assert.Throws<GameRuleException>(() => _service.Undo(id));

        Assert.Equal(GameErrorCode.NothingToUndo, ex.Code);
    }

    [Fact]
    public void Undo_AfterWin_RevertsScoreboard_AndReplayCountsOnce()
    {
        var id = _service.Create(GameMode.TwoPlayer).Id;
        PlayCells(id, 0, 3, 1, 4, 2);

        var undone = _service.Undo(id);
        Assert.Equal(GameStatus.InProgress, undone.Status);
        Assert.Equal(new ScoreboardDto(0, 0, 0), undone.Scoreboard);

        var replayed = _service.Play(id, Player.X, 0, 2);
        Assert.Equal(GameStatus.Won, replayed.Status);
        Assert.Equal(new ScoreboardDto(1, 0, 0), replayed.Scoreboard);
    }

    [Fact]
    public void Undo_AfterScoreboardReset_DoesNotDecrementNewScoreboard()
    {
        var id = _service.Create(GameMode.TwoPlayer).Id;
        PlayCells(id, 0, 3, 1, 4, 2);
        _service.ResetScoreboard();

        var state = _service.Undo(id);

        Assert.Equal(new ScoreboardDto(0, 0, 0), state.Scoreboard);
    }

    [Fact]
    public void ResetGame_KeepsScoreboard_AndStartsFresh()
    {
        var id = _service.Create(GameMode.TwoPlayer).Id;
        PlayCells(id, 0, 3, 1, 4, 2);

        var state = _service.Reset(id);

        Assert.Equal(id, state.Id);
        Assert.Equal(GameStatus.InProgress, state.Status);
        Assert.Empty(state.Moves);
        Assert.Empty(state.WinningCells);
        Assert.Null(state.Winner);
        Assert.Equal(Player.X, state.CurrentPlayer);
        Assert.Equal(new ScoreboardDto(1, 0, 0), state.Scoreboard);
    }

    [Fact]
    public void ResetScoreboard_ZeroesAllCounters()
    {
        var id = _service.Create(GameMode.TwoPlayer).Id;
        PlayCells(id, 0, 3, 1, 4, 2);

        var score = _service.ResetScoreboard();

        Assert.Equal(new ScoreboardDto(0, 0, 0), score);
        Assert.Equal(new ScoreboardDto(0, 0, 0), _service.Get(id).Scoreboard);
    }
}
