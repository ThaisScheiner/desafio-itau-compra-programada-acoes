import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Cotacoes } from './cotacoes';

describe('Cotacoes', () => {
  let component: Cotacoes;
  let fixture: ComponentFixture<Cotacoes>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Cotacoes],
    }).compileComponents();

    fixture = TestBed.createComponent(Cotacoes);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
