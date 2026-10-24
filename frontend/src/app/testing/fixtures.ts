import { GameState } from '../core/game.models';

/** Builds a backend-shaped game state for tests. */
export function makeState(overrides: Partial<GameState> = {}): GameState {
  return {
    id: 'g1',
    mode: 'TwoPlayer',
    board: [
      [null, null, null],
      [null, null, null],
      [null, null, null],
    ],
    currentPlayer: 'X',
    status: 'InProgress',
    winner: null,
    winningCells: [],
    moves: [],
    canUndo: false,
    scoreboard: { xWins: 0, oWins: 0, draws: 0 },
    ...overrides,
  };
}
