import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Rebalanceamentos } from './rebalanceamentos';

describe('Rebalanceamentos', () => {
  let component: Rebalanceamentos;
  let fixture: ComponentFixture<Rebalanceamentos>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Rebalanceamentos],
    }).compileComponents();

    fixture = TestBed.createComponent(Rebalanceamentos);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
