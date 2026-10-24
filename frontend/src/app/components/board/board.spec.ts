import { TestBed } from '@angular/core/testing';
import { Board } from './board';
import { Cell, Position } from '../../core/game.models';

describe('Board', () => {
  const board: Cell[][] = [
    ['X', 'X', 'X'],
    ['O', 'O', null],
    [null, null, null],
  ];

  function render(winningCells: Position[] = [], disabled = false) {
    const fixture = TestBed.createComponent(Board);
    fixture.componentRef.setInput('board', board);
    fixture.componentRef.setInput('winningCells', winningCells);
    fixture.componentRef.setInput('disabled', disabled);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const cell = (r: number, c: number) =>
      el.querySelector<HTMLButtonElement>(`[data-testid="cell-${r}-${c}"]`)!;
    return { fixture, el, cell };
  }

  it('renders a 3x3 grid with marks', () => {
    const { el, cell } = render();

    expect(el.querySelectorAll('.cell').length).toBe(9);
    expect(cell(0, 0).textContent?.trim()).toBe('X');
    expect(cell(1, 0).textContent?.trim()).toBe('O');
    expect(cell(2, 2).getAttribute('aria-label')).toBe('Row 3, Column 3: empty');
  });

  it('highlights only the winning cells', () => {
    const { el, cell } = render([
      { row: 0, column: 0 },
      { row: 0, column: 1 },
      { row: 0, column: 2 },
    ]);

    expect(el.querySelectorAll('.cell.winning').length).toBe(3);
    expect(cell(0, 2).classList).toContain('winning');
    expect(cell(1, 0).classList).not.toContain('winning');
  });

  it('emits the selected empty cell and disables occupied ones', () => {
    const { fixture, cell } = render();
    const selected: Position[] = [];
    fixture.componentInstance.cellSelected.subscribe((p) => selected.push(p));

    cell(1, 2).click();

    expect(selected).toEqual([{ row: 1, column: 2 }]);
    expect(cell(0, 0).disabled).toBe(true);
    expect(cell(1, 2).disabled).toBe(false);
  });

  it('disables every cell when the board is disabled', () => {
    const { el } = render([], true);

    const buttons = Array.from(el.querySelectorAll<HTMLButtonElement>('.cell'));
    expect(buttons.every((b) => b.disabled)).toBe(true);
  });
});
