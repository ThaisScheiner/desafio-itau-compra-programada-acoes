import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EventosIr } from './eventos-ir';

describe('EventosIr', () => {
  let component: EventosIr;
  let fixture: ComponentFixture<EventosIr>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EventosIr],
    }).compileComponents();

    fixture = TestBed.createComponent(EventosIr);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
