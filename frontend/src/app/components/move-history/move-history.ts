import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Move } from '../../core/game.models';

@Component({
  selector: 'app-move-history',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="card" aria-labelledby="history-title">
      <div class="card-header">
        <h2 id="history-title">Move History</h2>
        <span class="muted">{{ moves().length }} / 9</span>
      </div>
      @if (moves().length === 0) {
        <p class="muted empty">No moves yet.</p>
      } @else {
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th scope="col">Move</th>
                <th scope="col">Player</th>
                <th scope="col">Position</th>
              </tr>
            </thead>
            <tbody>
              @for (move of moves(); track move.number) {
                <tr>
                  <td>{{ move.number }}</td>
                  <td [class]="move.player === 'X' ? 'x' : 'o'">{{ move.player }}</td>
                  <td>{{ move.position }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>
  `,
  styles: `
    .table-wrap {
      max-height: 300px;
      overflow-y: auto;
    }
    table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.95rem;
    }
    th,
    td {
      text-align: left;
      padding: 6px 8px;
      border-bottom: 1px solid var(--border);
    }
    th {
      position: sticky;
      top: 0;
      background: var(--surface);
      color: var(--muted);
      font-weight: 600;
    }
    .x {
      color: var(--x-color);
      font-weight: 700;
    }
    .o {
      color: var(--o-color);
      font-weight: 700;
    }
    .empty {
      margin: 8px 0 0;
    }
  `,
})
export class MoveHistory {
  readonly moves = input.required<Move[]>();
}
