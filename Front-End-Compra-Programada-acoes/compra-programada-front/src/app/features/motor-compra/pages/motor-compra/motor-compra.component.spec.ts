import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MotorCompraComponent } from './motor-compra.component';

describe('MotorCompraComponent', () => {
  let component: MotorCompraComponent;
  let fixture: ComponentFixture<MotorCompraComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MotorCompraComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(MotorCompraComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
