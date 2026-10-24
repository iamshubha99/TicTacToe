import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { App } from './app';
import { makeState } from './testing/fixtures';

describe('App', () => {
  let fixture: ComponentFixture<App>;
  let http: HttpTestingController;
  let el: HTMLElement;

  const q = <T extends HTMLElement>(testId: string) =>
    el.querySelector<T>(`[data-testid="${testId}"]`)!;

  async function settle() {
    await fixture.whenStable();
    fixture.detectChanges();
  }

  beforeEach(async () => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(App);
    el = fixture.nativeElement;
    fixture.detectChanges();

    http.expectOne('/api/games').flush(makeState());
    await settle();
  });

  afterEach(() => http.verify());

  it('starts a two-player game and shows whose turn it is', () => {
    expect(q('status').textContent).toContain("Player X's turn");
    expect(q('mode-TwoPlayer').getAttribute('aria-checked')).toBe('true');
    expect(q<HTMLButtonElement>('undo').disabled).toBe(true);
    expect(el.querySelectorAll('.cell').length).toBe(9);
  });

  it('posts a move when a cell is clicked and renders the returned state', async () => {
    q<HTMLButtonElement>('cell-0-0').click();
    const req = http.expectOne('/api/games/g1/moves');
    expect(req.request.body).toEqual({ gameId: 'g1', player: 'X', row: 0, column: 0 });
    req.flush(
      makeState({
        board: [
          ['X', null, null],
          [null, null, null],
          [null, null, null],
        ],
        currentPlayer: 'O',
        canUndo: true,
        moves: [{ number: 1, player: 'X', row: 0, column: 0, position: 'Row 1, Column 1' }],
      }),
    );
    await settle();

    expect(q('status').textContent).toContain("Player O's turn");
    expect(q('cell-0-0').textContent?.trim()).toBe('X');
    expect(el.textContent).toContain('Row 1, Column 1');
    expect(q<HTMLButtonElement>('undo').disabled).toBe(false);
  });

  it('shows the winner, highlights cells, locks the board and updates the scoreboard', async () => {
    q<HTMLButtonElement>('cell-0-2').click();
    http.expectOne('/api/games/g1/moves').flush(
      makeState({
        board: [
          ['X', 'X', 'X'],
          ['O', 'O', null],
          [null, null, null],
        ],
        status: 'Won',
        winner: 'X',
        currentPlayer: null,
        winningCells: [
          { row: 0, column: 0 },
          { row: 0, column: 1 },
          { row: 0, column: 2 },
        ],
        canUndo: true,
        scoreboard: { xWins: 1, oWins: 0, draws: 0 },
      }),
    );
    await settle();

    expect(q('status').textContent).toContain('Player X wins');
    expect(el.querySelectorAll('.cell.winning').length).toBe(3);
    expect(q<HTMLButtonElement>('cell-2-2').disabled).toBe(true);
    expect(q('score-x').textContent?.trim()).toBe('1');
  });

  it('switching to computer mode creates a new computer game', async () => {
    q<HTMLButtonElement>('mode-Computer').click();
    const req = http.expectOne('/api/games');
    expect(req.request.body).toEqual({ mode: 'Computer' });
    req.flush(makeState({ id: 'g2', mode: 'Computer' }));
    await settle();

    expect(q('status').textContent).toContain('Your turn (X)');
    expect(q('mode-badge').textContent).toContain('Computer');
  });

  it('wires Undo, Reset Game and Reset Scoreboard to the API', async () => {
    fixture.componentInstance['store'].game.set(makeState({ canUndo: true }));
    fixture.detectChanges();

    q<HTMLButtonElement>('undo').click();
    http.expectOne('/api/games/g1/undo').flush(makeState());
    await settle();

    q<HTMLButtonElement>('reset-game').click();
    http.expectOne('/api/games/g1/reset').flush(makeState());
    await settle();

    el.querySelector<HTMLButtonElement>('app-scoreboard button')!.click();
    http.expectOne('/api/scoreboard/reset').flush({ xWins: 0, oWins: 0, draws: 0 });
    await settle();
  });

  it('shows backend validation errors', async () => {
    q<HTMLButtonElement>('cell-1-1').click();
    http
      .expectOne('/api/games/g1/moves')
      .flush({ detail: "It is O's turn, not X's." }, { status: 409, statusText: 'Conflict' });
    await settle();

    expect(q('error').textContent).toContain("It is O's turn");
  });
});
