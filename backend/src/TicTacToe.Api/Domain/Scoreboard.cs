namespace TicTacToe.Api.Domain;

public sealed record ScoreSnapshot(int XWins, int OWins, int Draws);

/// <summary>
/// Receipt for a result counted on the scoreboard. <see cref="Generation"/> ties the receipt to a
/// specific scoreboard "era" so that undoing a game after a scoreboard reset cannot decrement
/// the fresh scoreboard.
/// </summary>
public sealed record ScoreRecord(GameStatus Outcome, Player? Winner, int Generation);

/// <summary>Thread-safe, process-wide scoreboard.</summary>
public sealed class Scoreboard
{
    private readonly Lock _lock = new();
    private int _xWins;
    private int _oWins;
    private int _draws;
    private int _generation;

    public ScoreSnapshot Snapshot()
    {
        lock (_lock)
        {
            return new ScoreSnapshot(_xWins, _oWins, _draws);
        }
    }

    public ScoreRecord Record(GameStatus outcome, Player? winner)
    {
        lock (_lock)
        {
            Apply(outcome, winner, +1);
            return new ScoreRecord(outcome, winner, _generation);
        }
    }

    /// <summary>Reverses a previously recorded result. Returns false if the scoreboard was reset since.</summary>
    public bool Revert(ScoreRecord record)
    {
        lock (_lock)
        {
            if (record.Generation != _generation)
            {
                return false;
            }

            Apply(record.Outcome, record.Winner, -1);
            return true;
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _xWins = _oWins = _draws = 0;
            _generation++;
        }
    }

    private void Apply(GameStatus outcome, Player? winner, int delta)
    {
        switch (outcome)
        {
            case GameStatus.Won when winner == Player.X:
                _xWins = Math.Max(0, _xWins + delta);
                break;
            case GameStatus.Won when winner == Player.O:
                _oWins = Math.Max(0, _oWins + delta);
                break;
            case GameStatus.Draw:
                _draws = Math.Max(0, _draws + delta);
                break;
            default:
                throw new ArgumentException($"Cannot record outcome '{outcome}'.", nameof(outcome));
        }
    }
}
