import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { Board } from './components/board/board';
import { MoveHistory } from './components/move-history/move-history';
import { ScoreboardPanel } from './components/scoreboard/scoreboard';
import { GameMode } from './core/game.models';
import { GameStore } from './core/game.store';

@Component({
  selector: 'app-root',
  imports: [Board, MoveHistory, ScoreboardPanel],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  protected readonly store = inject(GameStore);

  protected readonly modes: { value: GameMode; label: string }[] = [
    { value: 'TwoPlayer', label: 'Two Player' },
    { value: 'Computer', label: 'Play Against Computer' },
  ];

  ngOnInit(): void {
    void this.store.init();
  }

  protected selectMode(mode: GameMode): void {
    if (mode !== this.store.mode() || !this.store.game()) {
      void this.store.newGame(mode);
    }
  }
}
