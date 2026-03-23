import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Custodias } from './custodias';

describe('Custodias', () => {
  let component: Custodias;
  let fixture: ComponentFixture<Custodias>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Custodias],
    }).compileComponents();

    fixture = TestBed.createComponent(Custodias);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
