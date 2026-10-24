using TicTacToe.Api.Domain;

namespace TicTacToe.Tests;

public class ScoreboardTests
{
    [Fact]
    public void Record_IncrementsMatchingCounter()
    {
        var scoreboard = new Scoreboard();

        scoreboard.Record(GameStatus.Won, Player.X);
        scoreboard.Record(GameStatus.Won, Player.O);
        scoreboard.Record(GameStatus.Won, Player.O);
        scoreboard.Record(GameStatus.Draw, null);

        Assert.Equal(new ScoreSnapshot(1, 2, 1), scoreboard.Snapshot());
    }

    [Fact]
    public void Revert_UndoesRecordedResult()
    {
        var scoreboard = new Scoreboard();
        var record = scoreboard.Record(GameStatus.Won, Player.X);

        Assert.True(scoreboard.Revert(record));
        Assert.Equal(new ScoreSnapshot(0, 0, 0), scoreboard.Snapshot());
    }

    [Fact]
    public void Reset_ZeroesCounters_AndOlderRecordsCannotBeReverted()
    {
        var scoreboard = new Scoreboard();
        var record = scoreboard.Record(GameStatus.Draw, null);

        scoreboard.Reset();
        scoreboard.Record(GameStatus.Draw, null);

        Assert.False(scoreboard.Revert(record));
        Assert.Equal(new ScoreSnapshot(0, 0, 1), scoreboard.Snapshot());
    }

    [Fact]
    public void Record_InProgress_IsRejected()
    {
        var scoreboard = new Scoreboard();

        Assert.Throws<ArgumentException>(() => scoreboard.Record(GameStatus.InProgress, null));
    }
}
