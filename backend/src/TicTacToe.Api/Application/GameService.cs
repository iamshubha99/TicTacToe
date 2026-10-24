using TicTacToe.Api.Contracts;
using TicTacToe.Api.Domain;

namespace TicTacToe.Api.Application;

/// <summary>
/// Orchestrates game sessions, the computer opponent and the scoreboard.
/// Every operation runs under the game's lock so a request sees a consistent state.
/// </summary>
public sealed class GameService(IGameSessionStore store, Scoreboard scoreboard)
{
    public GameStateResponse Create(GameMode mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new GameRuleException(GameErrorCode.InvalidRequest, $"Unknown game mode '{mode}'.");
        }

        var session = new GameSession(new Game(Guid.NewGuid(), mode));
        store.Add(session);
        return session.Game.ToResponse(scoreboard.Snapshot());
    }

    public GameStateResponse Get(Guid id) => WithSession(id, session => { });

    public GameStateResponse Play(Guid id, Player player, int row, int column) => WithSession(id, session =>
    {
        var game = session.Game;
        game.Play(player, new Position(row, column));
        RecordIfCompleted(session);

        if (game.Mode == GameMode.Computer && !game.IsCompleted && game.CurrentPlayer == Game.ComputerPlayer)
        {
            game.Play(Game.ComputerPlayer, ComputerStrategy.ChooseMove(game.Board, Game.ComputerPlayer));
            RecordIfCompleted(session);
        }
    });

    /// <summary>Undo is allowed after completion (README "Option B"); a reversed result is removed from the scoreboard.</summary>
    public GameStateResponse Undo(Guid id) => WithSession(id, session =>
    {
        session.Game.Undo();

        // Any prefix of a completed game is in progress, so a counted result is always reversed here.
        if (session.RecordedResult is { } recorded && !session.Game.IsCompleted)
        {
            scoreboard.Revert(recorded);
            session.RecordedResult = null;
        }
    });

    /// <summary>Clears the board and history but keeps the scoreboard (and any result already counted).</summary>
    public GameStateResponse Reset(Guid id) => WithSession(id, session =>
    {
        session.Game.Reset();
        session.RecordedResult = null;
    });

    public ScoreboardDto GetScoreboard() => scoreboard.Snapshot().ToDto();

    public ScoreboardDto ResetScoreboard()
    {
        scoreboard.Reset();
        return scoreboard.Snapshot().ToDto();
    }

    private void RecordIfCompleted(GameSession session)
    {
        var game = session.Game;
        if (game.IsCompleted && session.RecordedResult is null)
        {
            session.RecordedResult = scoreboard.Record(game.Status, game.Winner);
        }
    }

    private GameStateResponse WithSession(Guid id, Action<GameSession> action)
    {
        var session = store.Find(id) ?? throw new GameNotFoundException(id);
        lock (session.SyncRoot)
        {
            action(session);
            return session.Game.ToResponse(scoreboard.Snapshot());
        }
    }
}
