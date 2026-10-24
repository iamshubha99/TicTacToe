import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { makeState } from '../testing/fixtures';
import { describeStatus, GameStore } from './game.store';

describe('describeStatus', () => {
  it('describes turns, wins and draws for both modes', () => {
    expect(describeStatus(makeState({ currentPlayer: 'O' }))).toBe("Player O's turn");
    expect(describeStatus(makeState({ mode: 'Computer' }))).toBe('Your turn (X)');
    expect(
      describeStatus(makeState({ status: 'Won', winner: 'O', currentPlayer: null })),
    ).toContain('Player O wins');
    expect(describeStatus(makeState({ mode: 'Computer', status: 'Won', winner: 'O' }))).toBe(
      'Computer wins!',
    );
    expect(describeStatus(makeState({ status: 'Draw', currentPlayer: null }))).toBe("It's a draw!");
  });
});

describe('GameStore', () => {
  let store: GameStore;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    store = TestBed.inject(GameStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('creates a new two-player game when nothing is saved', async () => {
    const init = store.init();
    http.expectOne('/api/games').flush(makeState({ id: 'new' }));
    await init;

    expect(store.game()?.id).toBe('new');
    expect(localStorage.getItem('tictactoe.gameId')).toBe('new');
  });

  it('resumes a saved game, and falls back to a new game if it no longer exists', async () => {
    localStorage.setItem('tictactoe.gameId', 'stale');
    const init = store.init();

    http
      .expectOne('/api/games/stale')
      .flush({ code: 'GameNotFound' }, { status: 404, statusText: 'Not Found' });
    await Promise.resolve();
    await new Promise((r) => setTimeout(r));
    http.expectOne('/api/games').flush(makeState({ id: 'fresh' }));
    await init;

    expect(store.game()?.id).toBe('fresh');
    expect(store.error()).toBeNull();
  });

  it('sends the current player with the move and renders the backend response', async () => {
    store.game.set(makeState());
    const play = store.play(0, 0);

    const req = http.expectOne('/api/games/g1/moves');
    expect(req.request.body).toEqual({ gameId: 'g1', player: 'X', row: 0, column: 0 });
    req.flush(
      makeState({
        currentPlayer: 'O',
        canUndo: true,
        scoreboard: { xWins: 2, oWins: 1, draws: 0 },
      }),
    );
    await play;

    expect(store.game()?.currentPlayer).toBe('O');
    expect(store.scoreboard()).toEqual({ xWins: 2, oWins: 1, draws: 0 });
    expect(store.canUndo()).toBe(true);
  });

  it('surfaces the problem detail when the backend rejects a move and keeps the state', async () => {
    const original = makeState();
    store.game.set(original);
    const play = store.play(0, 0);

    http
      .expectOne('/api/games/g1/moves')
      .flush(
        { detail: 'Cell Row 1, Column 1 is already occupied.', code: 'CellOccupied' },
        { status: 409, statusText: 'Conflict' },
      );
    expect(await play).toBe(false);

    expect(store.error()).toBe('Cell Row 1, Column 1 is already occupied.');
    expect(store.game()).toBe(original);
  });

  it('does not send moves once the game is completed', async () => {
    store.game.set(makeState({ status: 'Won', winner: 'X', currentPlayer: null }));

    expect(await store.play(1, 1)).toBe(false);
    http.expectNone('/api/games/g1/moves');
  });

  it('updates the scoreboard on reset', async () => {
    store.scoreboard.set({ xWins: 3, oWins: 2, draws: 1 });
    const reset = store.resetScoreboard();
    http.expectOne('/api/scoreboard/reset').flush({ xWins: 0, oWins: 0, draws: 0 });
    await reset;

    expect(store.scoreboard()).toEqual({ xWins: 0, oWins: 0, draws: 0 });
  });
});
