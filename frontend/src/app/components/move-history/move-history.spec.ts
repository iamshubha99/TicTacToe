import { TestBed } from '@angular/core/testing';
import { MoveHistory } from './move-history';

describe('MoveHistory', () => {
  it('shows an empty message when there are no moves', () => {
    const fixture = TestBed.createComponent(MoveHistory);
    fixture.componentRef.setInput('moves', []);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No moves yet.');
  });

  it('lists move number, player and position', () => {
    const fixture = TestBed.createComponent(MoveHistory);
    fixture.componentRef.setInput('moves', [
      { number: 1, player: 'X', row: 0, column: 0, position: 'Row 1, Column 1' },
      { number: 2, player: 'O', row: 1, column: 1, position: 'Row 2, Column 2' },
    ]);
    fixture.detectChanges();

    const rows = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr'),
    ).map((tr) => Array.from(tr.querySelectorAll('td')).map((td) => td.textContent?.trim()));
    expect(rows).toEqual([
      ['1', 'X', 'Row 1, Column 1'],
      ['2', 'O', 'Row 2, Column 2'],
    ]);
  });
});
