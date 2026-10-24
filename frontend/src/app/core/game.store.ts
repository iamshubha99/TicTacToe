import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom, Observable } from 'rxjs';
import { GameApiService } from './game-api.service';
import { ApiProblem, GameMode, GameState, Scoreboard } from './game.models';

const GAME_ID_KEY = 'tictactoe.gameId';

/**
 * Holds the latest state returned by the backend. The backend is the source of truth:
 * the store never applies game rules itself, it only renders what the API returns.
 */
@Injectable({ providedIn: 'root' })
export class GameStore {
  private readonly api = inject(GameApiService);

  readonly game = signal<GameState | null>(null);
  readonly scoreboard = signal<Scoreboard>({ xWins: 0, oWins: 0, draws: 0 });
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly mode = computed<GameMode>(() => this.game()?.mode ?? 'TwoPlayer');
  readonly isCompleted = computed(() => {
    const status = this.game()?.status;
    return status === 'Won' || status === 'Draw';
  });
  readonly canUndo = computed(() => !this.busy() && !!this.game()?.canUndo);
  readonly statusMessage = computed(() => describeStatus(this.game()));

  /** Resumes the game from a previous page load if the backend still knows it, otherwise starts a new one. */
  async init(): Promise<void> {
    const savedId = readSavedGameId();
    if (savedId) {
      const resumed = await this.run(() => this.api.getGame(savedId), { silent: true });
      if (resumed) {
        return;
      }
    }

    await this.newGame('TwoPlayer');
  }

  newGame(mode: GameMode): Promise<boolean> {
    return this.run(() => this.api.createGame(mode));
  }

  play(row: number, column: number): Promise<boolean> {
    const game = this.game();
    if (!game || !game.currentPlayer || this.busy()) {
      return Promise.resolve(false);
    }

    return this.run(() =>
      this.api.makeMove({ gameId: game.id, player: game.currentPlayer!, row, column }),
    );
  }

  undo(): Promise<boolean> {
    const game = this.game();
    return game ? this.run(() => this.api.undo(game.id)) : Promise.resolve(false);
  }

  resetGame(): Promise<boolean> {
    const game = this.game();
    return game ? this.run(() => this.api.resetGame(game.id)) : Promise.resolve(false);
  }

  async resetScoreboard(): Promise<boolean> {
    this.busy.set(true);
    this.error.set(null);
    try {
      this.scoreboard.set(await firstValueFrom(this.api.resetScoreboard()));
      return true;
    } catch (err) {
      this.error.set(toMessage(err));
      return false;
    } finally {
      this.busy.set(false);
    }
  }

  clearError(): void {
    this.error.set(null);
  }

  private async run(
    request: () => Observable<GameState>,
    options: { silent?: boolean } = {},
  ): Promise<boolean> {
    this.busy.set(true);
    this.error.set(null);
    try {
      const state = await firstValueFrom(request());
      this.game.set(state);
      this.scoreboard.set(state.scoreboard);
      saveGameId(state.id);
      return true;
    } catch (err) {
      if (!options.silent) {
        this.error.set(toMessage(err));
      }
      return false;
    } finally {
      this.busy.set(false);
    }
  }
}

export function describeStatus(game: GameState | null): string {
  if (!game) {
    return 'Starting a new game…';
  }

  const vsComputer = game.mode === 'Computer';
  switch (game.status) {
    case 'Won':
      if (vsComputer) {
        return game.winner === 'X' ? 'You win! 🎉' : 'Computer wins!';
      }
      return `Player ${game.winner} wins! 🎉`;
    case 'Draw':
      return "It's a draw!";
    default:
      return vsComputer ? 'Your turn (X)' : `Player ${game.currentPlayer}'s turn`;
  }
}

function toMessage(err: unknown): string {
  if (err instanceof HttpErrorResponse) {
    if (err.status === 0) {
      return 'Cannot reach the game server. Is the backend running on http://localhost:5080?';
    }
    const problem = err.error as ApiProblem | null;
    return problem?.detail ?? problem?.title ?? `Request failed (${err.status}).`;
  }
  return 'Something went wrong.';
}

function readSavedGameId(): string | null {
  try {
    return localStorage.getItem(GAME_ID_KEY);
  } catch {
    return null;
  }
}

function saveGameId(id: string): void {
  try {
    localStorage.setItem(GAME_ID_KEY, id);
  } catch {
    // Storage may be unavailable (e.g. private mode); resuming is a nice-to-have.
  }
}
