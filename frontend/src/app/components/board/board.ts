import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { Cell, Position } from '../../core/game.models';

@Component({
  selector: 'app-board',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="board" role="grid" aria-label="Tic Tac Toe board">
      @for (row of board(); track $index; let r = $index) {
        <div class="board-row" role="row">
          @for (cell of row; track $index; let c = $index) {
            <button
              type="button"
              role="gridcell"
              class="cell"
              [class.x]="cell === 'X'"
              [class.o]="cell === 'O'"
              [class.winning]="isWinning(r, c)"
              [disabled]="disabled() || cell !== null"
              [attr.aria-label]="
                'Row ' + (r + 1) + ', Column ' + (c + 1) + ': ' + (cell ?? 'empty')
              "
              [attr.data-testid]="'cell-' + r + '-' + c"
              (click)="cellSelected.emit({ row: r, column: c })"
            >
              {{ cell ?? '' }}
            </button>
          }
        </div>
      }
    </div>
  `,
  styleUrl: './board.scss',
})
export class Board {
  readonly board = input.required<Cell[][]>();
  readonly winningCells = input<Position[]>([]);
  readonly disabled = input(false);

  readonly cellSelected = output<Position>();

  private readonly winningKeys = computed(
    () => new Set(this.winningCells().map((p) => `${p.row},${p.column}`)),
  );

  protected isWinning(row: number, column: number): boolean {
    return this.winningKeys().has(`${row},${column}`);
  }
}
