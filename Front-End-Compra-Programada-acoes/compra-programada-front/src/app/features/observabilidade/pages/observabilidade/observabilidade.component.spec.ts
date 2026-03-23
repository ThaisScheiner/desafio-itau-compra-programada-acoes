import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ObservabilidadeComponent } from './observabilidade.component';

describe('ObservabilidadeComponent', () => {
  let component: ObservabilidadeComponent;
  let fixture: ComponentFixture<ObservabilidadeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ObservabilidadeComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ObservabilidadeComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
