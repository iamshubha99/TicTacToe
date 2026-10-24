using System.Collections.Concurrent;
using TicTacToe.Api.Domain;

namespace TicTacToe.Api.Application;

/// <summary>A game plus the bookkeeping needed to keep the scoreboard consistent with it.</summary>
public sealed class GameSession(Game game)
{
    public Game Game { get; } = game;

    /// <summary>Set when this game's result has been counted on the scoreboard.</summary>
    public ScoreRecord? RecordedResult { get; set; }

    public Lock SyncRoot { get; } = new();
}

public interface IGameSessionStore
{
    void Add(GameSession session);

    GameSession? Find(Guid id);
}

public sealed class InMemoryGameSessionStore : IGameSessionStore
{
    private readonly ConcurrentDictionary<Guid, GameSession> _sessions = new();

    public void Add(GameSession session) => _sessions[session.Game.Id] = session;

    public GameSession? Find(Guid id) => _sessions.GetValueOrDefault(id);
}
