import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { GameApiService } from './game-api.service';
import { makeState } from '../testing/fixtures';

describe('GameApiService', () => {
  let api: GameApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(GameApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('creates a game with the selected mode', async () => {
    const result = firstValueFrom(api.createGame('Computer'));
    const req = http.expectOne('/api/games');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ mode: 'Computer' });
    req.flush(makeState({ mode: 'Computer' }));
    expect((await result).mode).toBe('Computer');
  });

  it('gets a game by id', () => {
    api.getGame('g1').subscribe();
    expect(http.expectOne('/api/games/g1').request.method).toBe('GET');
  });

  it('submits a move with game id, player, row and column', () => {
    api.makeMove({ gameId: 'g1', player: 'X', row: 1, column: 2 }).subscribe();
    const req = http.expectOne('/api/games/g1/moves');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ gameId: 'g1', player: 'X', row: 1, column: 2 });
  });

  it('calls undo, reset and scoreboard endpoints', () => {
    api.undo('g1').subscribe();
    api.resetGame('g1').subscribe();
    api.getScoreboard().subscribe();
    api.resetScoreboard().subscribe();

    expect(http.expectOne('/api/games/g1/undo').request.method).toBe('POST');
    expect(http.expectOne('/api/games/g1/reset').request.method).toBe('POST');
    expect(http.expectOne('/api/scoreboard').request.method).toBe('GET');
    expect(http.expectOne('/api/scoreboard/reset').request.method).toBe('POST');
  });
});
