export type Player = 'X' | 'O';
export type GameMode = 'TwoPlayer' | 'Computer';
export type GameStatus = 'InProgress' | 'Won' | 'Draw';
export type Cell = Player | null;

/** Zero-based board coordinate, as used by the API. */
export interface Position {
  row: number;
  column: number;
}

export interface Move {
  number: number;
  player: Player;
  row: number;
  column: number;
  /** One-based, human-friendly label, e.g. "Row 1, Column 1". */
  position: string;
}

export interface Scoreboard {
  xWins: number;
  oWins: number;
  draws: number;
}

export interface GameState {
  id: string;
  mode: GameMode;
  board: Cell[][];
  /** Null once the game is completed. */
  currentPlayer: Player | null;
  status: GameStatus;
  winner: Player | null;
  winningCells: Position[];
  moves: Move[];
  canUndo: boolean;
  scoreboard: Scoreboard;
}

export interface MoveRequest {
  gameId: string;
  player: Player;
  row: number;
  column: number;
}

/** RFC 7807 problem response returned by the API for rejected actions. */
export interface ApiProblem {
  title?: string;
  detail?: string;
  status?: number;
  code?: string;
}
