# Tic Tac Toe — Angular + .NET

A browser-based Tic Tac Toe game. The **Angular** frontend renders the game, and the **.NET** backend owns all game state and rules. They talk over a small REST API.

Features: Two Player mode, Play Against Computer mode, move history, mode-aware Undo, a session scoreboard, Reset Game, and Reset Scoreboard.

---

## 1. Project overview

| Layer | Responsibility |
|---|---|
| **Backend (`backend/`)** | The source of truth. It stores game sessions in memory, validates every move, detects wins and draws, picks the computer's moves, applies Undo and Reset, and keeps the scoreboard. |
| **Frontend (`frontend/`)** | A thin UI. It sends user actions to the API and renders the state that comes back. It never applies game rules on its own. |

```
demo/
├── backend/
│   ├── TicTacToe.slnx
│   ├── src/TicTacToe.Api/
│   │   ├── Domain/          # Pure game rules (no ASP.NET dependencies)
│   │   │   ├── Game.cs              # moves → board/turn/status, Undo, Reset
│   │   │   ├── Board.cs, WinDetector.cs, Primitives.cs
│   │   │   ├── ComputerStrategy.cs  # win → block → center → corner → any
│   │   │   ├── Scoreboard.cs        # thread-safe counters + revert support
│   │   │   └── GameRuleException.cs # typed rule violations (error codes)
│   │   ├── Application/     # GameService (orchestration), in-memory session store
│   │   ├── Contracts/       # Request/response DTOs + mapping
│   │   ├── Endpoints/       # Minimal API routes + ProblemDetails exception handler
│   │   ├── Program.cs
│   │   └── TicTacToe.Api.http   # Ready-to-run sample requests (VS / VS Code REST Client)
│   └── tests/TicTacToe.Tests/   # xUnit: domain, service, and HTTP integration tests
└── frontend/
    ├── proxy.conf.json          # /api → http://localhost:5080
    └── src/app/
        ├── core/                # models, GameApiService (HTTP), GameStore (signals)
        ├── components/          # board, scoreboard, move-history
        ├── testing/fixtures.ts  # test data builder
        └── app.ts / app.html    # page layout
```

## 2. Tech stack

- **Backend:** .NET 10 (LTS), ASP.NET Core Minimal APIs, built-in OpenAPI document, RFC 7807 ProblemDetails
- **Backend tests:** xUnit, `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`)
- **Frontend:** Angular 22 (standalone components, signals, zoneless change detection, new control flow), TypeScript, SCSS
- **Frontend tests:** Vitest via the Angular CLI `unit-test` builder, `HttpTestingController`

## 3. Features implemented

- **Game board:** a standard 3×3 grid. Occupied cells and finished games are not clickable.
- **Turns:** X always starts and players alternate. A status line shows whose turn it is. Rejected moves leave the turn unchanged.
- **Win detection:** checks all 3 rows, 3 columns, and 2 diagonals. The app shows the winner, highlights the winning cells, locks the board, and updates the scoreboard.
- **Draw detection:** when all 9 cells are filled with no winner, the app shows a draw message, locks the board, and updates the scoreboard.
- **Reset Game:** clears the board, move history, and result, and gives the turn back to X. The scoreboard is not changed.
- **Move history:** a table of move number, player, and position (for example, "Row 1, Column 1"). It updates after every move.
- **Undo Last Move:**
  - Two Player mode removes only the latest move.
  - Computer mode removes the computer's move and the human move before it.
  - The button is disabled when there are no moves.
- **Scoreboard:** counts X wins, O wins, and draws. It is served by the backend and has its own Reset Scoreboard button.
- **Computer mode:** the human plays X and the computer plays O. The computer replies in the same API call. It picks moves in this order: win, block, center, corner, then any free cell.
- **Error handling:** when the backend rejects an action, the UI shows its message (for example, "Cell Row 2, Column 2 is already occupied."). If the API is unreachable, the UI says so clearly.
- **Resume:** the current game ID is saved in `localStorage`, so refreshing the page resumes the game while the API is still running.

## 4. Run the backend

**Prerequisite:** [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

```bash
cd backend
dotnet run --project src/TicTacToe.Api
```

- The API listens on **http://localhost:5080** (set in `Properties/launchSettings.json`).
- The OpenAPI document is at http://localhost:5080/openapi/v1.json (Development environment only).
- `src/TicTacToe.Api/TicTacToe.Api.http` has sample calls for every endpoint.

## 5. Run the frontend

**Prerequisite:** Node.js 20.19+ (tested with Node 24) and npm

```bash
cd frontend
npm install
npm start          # = ng serve, http://localhost:4200
```

Open **http://localhost:4200**. The dev server proxies `/api/*` to `http://localhost:5080` through `proxy.conf.json`, so start the backend first.

CORS is also enabled for `http://localhost:4200`. That lets the UI call the API directly if you set `API_BASE_URL`. The allowed origins are configured in `appsettings.json` under `Cors:AllowedOrigins`.

## 6. API endpoint summary

Base URL: `http://localhost:5080`. Enum values are sent as strings. **Row and column are zero-based (0–2).** The `position` label in the move history is one-based for display.

| Method | Endpoint | Body | Success | Purpose |
|---|---|---|---|---|
| POST | `/api/games` | `{ "mode": "TwoPlayer" \| "Computer" }` (optional, default `TwoPlayer`) | `201` + GameState | Create a new game session |
| GET | `/api/games/{id}` | — | `200` + GameState | Get the current game state |
| POST | `/api/games/{id}/moves` | `{ "gameId": "…", "player": "X", "row": 0, "column": 2 }` | `200` + GameState | Submit a move. In Computer mode, the computer's reply is already applied in the response. |
| POST | `/api/games/{id}/undo` | — | `200` + GameState | Undo (behavior depends on mode) |
| POST | `/api/games/{id}/reset` | — | `200` + GameState | Reset the board. Same game ID and mode, scoreboard kept. |
| GET | `/api/scoreboard` | — | `200` + Scoreboard | Get the scoreboard |
| POST | `/api/scoreboard/reset` | — | `200` + Scoreboard | Reset the scoreboard |

`gameId` in the move body is optional. If you send it, it must match the ID in the URL.

**GameState response**

```json
{
  "id": "941af60a-a0e2-4d2c-8ad3-eef66b00bb59",
  "mode": "Computer",
  "board": [["X", null, null], [null, "O", null], [null, null, null]],
  "currentPlayer": "X",
  "status": "InProgress",
  "winner": null,
  "winningCells": [],
  "moves": [
    { "number": 1, "player": "X", "row": 0, "column": 0, "position": "Row 1, Column 1" },
    { "number": 2, "player": "O", "row": 1, "column": 1, "position": "Row 2, Column 2" }
  ],
  "canUndo": true,
  "scoreboard": { "xWins": 0, "oWins": 0, "draws": 0 }
}
```

- `status` is one of `InProgress`, `Won`, or `Draw`.
- `currentPlayer` is `null` once the game is completed.
- `winningCells` is a list of `{ row, column }` objects.

**Errors.** Errors use RFC 7807 `application/problem+json` with an extra machine-readable `code`:

```json
{ "title": "Action not allowed", "status": 409, "detail": "Cell Row 2, Column 2 is already occupied.", "code": "CellOccupied" }
```

| HTTP | `code` | When |
|---|---|---|
| 400 | `OutOfBounds` | Row or column outside 0–2 |
| 400 | `InvalidRequest` | Missing `player`, `row`, or `column`; `gameId` doesn't match the URL; unknown mode |
| 404 | `GameNotFound` | Unknown game ID |
| 409 | `CellOccupied` | The cell already has a mark |
| 409 | `WrongPlayer` | It's not this player's turn. Also used for O moves in Computer mode, because O belongs to the computer. |
| 409 | `GameCompleted` | Move made after a win or draw |
| 409 | `NothingToUndo` | Undo with an empty history |

## 7. Run the tests

```bash
# Backend: 69 tests (domain rules, service orchestration, HTTP integration)
cd backend
dotnet test

# Frontend: 23 tests (API service, store, components, app integration)
cd frontend
npm test -- --watch=false
```

How the required scenarios are covered:

| Scenario | Where |
|---|---|
| Valid move, turn switching | `GameTests.Play_ValidMove_*`, `Play_AlternatesTurns`, `ApiTests.ValidMove_*` |
| Invalid moves (out of bounds, occupied, wrong player) | `GameTests.Play_OutsideBoard_*`, `Play_OnOccupiedCell_*`, `Play_ByWrongPlayer_*`, `ApiTests.InvalidMove_*` |
| Row, column, and diagonal wins | `GameTests.RowWin_*` (all 3 rows), `ColumnWin_*` (all 3 columns), `MainDiagonalWin_*`, `AntiDiagonalWin_*` |
| Draw | `GameTests.FullBoardWithoutWinner_IsDraw`, `WinOnLastCell_IsWinNotDraw`, `GameServiceTests.Draw_*` |
| Move after completion | `GameTests.Play_AfterWin_*`, `Play_AfterDraw_*`, `GameServiceTests.Win_UpdatesScoreboardExactlyOnce` |
| Reset game | `GameTests.Reset_*`, `GameServiceTests.ResetGame_KeepsScoreboard_*` |
| Undo, Two Player | `GameTests.Undo_TwoPlayer_*`, `GameServiceTests.Undo_TwoPlayer_*` |
| Undo, Computer | `GameTests.Undo_ComputerMode_*`, `GameServiceTests.Undo_ComputerMode_*` |
| Scoreboard update and reset | `ScoreboardTests`, `GameServiceTests.Win_*`, `Undo_AfterWin_*`, `Undo_AfterScoreboardReset_*`, `ResetScoreboard_*` |
| Computer move selection | `ComputerStrategyTests` (win, block, win-over-block, center, corner, any, 500 random games never pick an occupied cell), plus the service-level computer win and loss tests |
| Frontend rendering and API integration | `board.spec.ts`, `move-history.spec.ts`, `game-api.service.spec.ts`, `game.store.spec.ts`, `app.spec.ts` |

## 8. AI tools and prompt summary

- **Tool used:** GitHub Copilot (agent mode in VS Code).
- **Prompt:** the full exercise brief, which covers the problem statement, functional requirements, API scope, clarifications, testing expectations, and README expectations.

**How the brief became a specification:**

1. Turned each requirement into backend behavior: rules, the state machine, and error codes.
2. Wrote down the REST contract, including request and response shapes and status codes.
3. Mapped each UI element to the API calls it uses.
4. Mapped each required test scenario to concrete test cases. The table in section 7 is the result.

**What the AI generated:**

- Project scaffolding through the official templates (`dotnet new webapi` and `xunit`, `ng new`).
- The domain model, service, endpoints, Angular store and components, both test suites, and this README.

**Issues caught during review and testing:**

- One hand-written test sequence produced an early O win before the intended final X move. The board layouts were then traced by hand and corrected.
- A shared test fixture was first exported from a `.spec` file, which made that suite run three times. It was moved to `testing/fixtures.ts`.
- A browser check showed the third board row rendered shorter than the others. The fix was an explicit `grid-template-rows`.

**Parts that deserve careful review:**

- The Undo rules for each mode.
- The scoreboard and Undo interaction, including the generation guard (see section 9).
- The computer priority order and how it breaks ties.
- Thread safety (one lock per game, plus a lock on the scoreboard).
- The HTTP status-code mapping.

> **Reviewer / candidate note:** add your own notes here on what you changed by hand, which prompts you iterated on, and anything you would do differently.

## 9. Design decisions

- **The move list is the single source of truth.** The board, turn, status, winner, and winning cells are all derived from the list of moves. Undo removes moves and then *replays* the rest, so the win and draw status is always recalculated. Undo can never leave the game in an inconsistent state.
- **The domain layer has no framework code.** `Domain/` has no ASP.NET dependencies, so the game rules are tested with plain unit tests. `GameService` coordinates the game, the computer, and the scoreboard. The endpoints are thin.
- **The computer replies inside the move request.** In Computer mode, `POST /moves` applies the human move and then the computer move under the same lock, and returns the final state. There's no time when it is O's turn and a client could send an O move. The computer never moves after the game ends.
- **The computer is deterministic.** Its priority order is win, block, center, corner (top-left, top-right, bottom-left, bottom-right), then the first free cell. Ties resolve in a fixed order, which makes tests reliable and behavior easy to explain.
- **Typed rule errors become ProblemDetails.** `GameRuleException(code)` is mapped centrally to 400, 404, or 409 with a `code`. Endpoints contain no try/catch blocks.
- **The frontend is a thin client built on signals.** `GameStore` holds the latest `GameState` from the API, and components only take inputs and emit events. The UI disables invalid actions (occupied cells, a finished board, Undo with no history) to make it easier to use. The backend still validates everything.
- **The dev proxy avoids CORS.** The frontend calls relative `/api` URLs, and the Angular dev server forwards them. CORS for `localhost:4200` is still configured on the API.

## 10. Clarifications and assumptions

- **Scoreboard and Undo: Option B (Undo is allowed after a game ends).**
  - Undoing a finished game removes its result from the scoreboard. If the game is then replayed to an end, the result is counted again. Each game only ever counts once.
  - Each game keeps a receipt (`ScoreRecord`) of the result it added to the scoreboard.
  - The scoreboard has a *generation* number that goes up on every Reset Scoreboard. If you reset the scoreboard and then undo an older finished game, the fresh scoreboard is not decremented, so it can never go negative.
- **Undo in Computer mode when the human moved last.** This happens when X makes the winning move or fills the board. There is no computer move to remove in that case, so only X's last move is removed. Afterwards it is always X's turn.
- **Reset Game** keeps the same game ID and mode. It counts as a fresh game: the board, history, and result are cleared and X starts. A result that was already counted on the scoreboard stays counted.
- **Switching mode** in the UI starts a new game in that mode, and the scoreboard is kept. Any game in progress is abandoned.
- **One scoreboard per API process.** There is one scoreboard for the whole API process, shared by all games. "Session" means the lifetime of the running API, and a single local user is assumed.
- **Coordinates** are zero-based in the API and one-based in the UI labels.
- **X always starts**, including in Computer mode, where the human is X.

## 11. Known limitations

- State lives only in memory. Restarting the API clears all games and the scoreboard, and old games are never cleaned up.
- There is no authentication or per-user isolation. Anyone who can reach the API shares the scoreboard.
- The computer follows the required simple priority list, not minimax, so it can be beaten with a fork.
- The computer has no "thinking" delay or animation. Its move appears together with yours.
- There is no end-to-end browser test suite. Frontend tests run in jsdom with a mocked HTTP layer.

## 12. Future improvements

- Persist sessions and the scoreboard (SQLite or EF Core, or Redis), and expire idle games.
- Add a scoreboard per user or browser session, using a cookie or session ID.
- Add an optional unbeatable "hard" difficulty using minimax.
- Add Playwright end-to-end tests that run against the real API, plus a CI workflow (GitHub Actions) that runs both test suites.
- Add Docker Compose to start both apps with one command.
- Add real-time two-device play using SignalR.
