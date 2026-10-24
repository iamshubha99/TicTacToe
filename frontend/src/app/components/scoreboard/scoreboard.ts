import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { Scoreboard } from '../../core/game.models';

@Component({
  selector: 'app-scoreboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="card" aria-labelledby="scoreboard-title">
      <div class="card-header">
        <h2 id="scoreboard-title">Scoreboard</h2>
        <button
          type="button"
          class="btn btn-small"
          [disabled]="disabled()"
          (click)="resetScoreboard.emit()"
        >
          Reset Scoreboard
        </button>
      </div>
      <dl class="scores">
        <div class="score x">
          <dt>X wins</dt>
          <dd data-testid="score-x">{{ score().xWins }}</dd>
        </div>
        <div class="score">
          <dt>Draws</dt>
          <dd data-testid="score-draws">{{ score().draws }}</dd>
        </div>
        <div class="score o">
          <dt>O wins</dt>
          <dd data-testid="score-o">{{ score().oWins }}</dd>
        </div>
      </dl>
    </section>
  `,
  styles: `
    .scores {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 8px;
      margin: 0;
    }
    .score {
      text-align: center;
      padding: 10px 4px;
      border-radius: 10px;
      background: var(--surface-muted);
    }
    dt {
      font-size: 0.8rem;
      color: var(--muted);
    }
    dd {
      margin: 4px 0 0;
      font-size: 1.6rem;
      font-weight: 700;
    }
    .x dd {
      color: var(--x-color);
    }
    .o dd {
      color: var(--o-color);
    }
  `,
})
export class ScoreboardPanel {
  readonly score = input.required<Scoreboard>();
  readonly disabled = input(false);

  readonly resetScoreboard = output<void>();
}
