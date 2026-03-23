import { ChangeDetectorRef, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { CestasService } from '../../../../core/services/cestas.service';

@Component({
  selector: 'app-cesta-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
  ],
  templateUrl: './cestas-form.component.html',
  styleUrl: './cestas-form.component.scss',
})
export class CestaFormComponent {
  saving = false;

  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    private cestasService: CestasService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {
    this.form = this.fb.group({
      nome: ['', [Validators.required, Validators.minLength(3)]],
      itens: this.fb.array([
        this.criarItemForm(),
        this.criarItemForm(),
        this.criarItemForm(),
        this.criarItemForm(),
        this.criarItemForm(),
      ]),
    });
  }

  get itens(): FormArray {
    return this.form.get('itens') as FormArray;
  }

  get totalPercentual(): number {
    return this.itens.controls.reduce((total, control) => {
      const valor = Number(control.get('percentual')?.value || 0);
      return total + valor;
    }, 0);
  }

  get erroPercentual(): boolean {
    return this.itens.length > 0 && this.totalPercentual !== 100;
  }

  adicionarItem(): void {
    if (this.itens.length >= 5) {
      alert('A cesta deve conter exatamente 5 ativos.');
      return;
    }

    this.itens.push(this.criarItemForm());
    this.cdr.detectChanges();
  }

  removerItem(index: number): void {
    if (this.itens.length <= 1) {
      return;
    }

    this.itens.removeAt(index);
    this.cdr.detectChanges();
  }

  salvar(): void {
    this.form.markAllAsTouched();

    if (this.form.invalid) {
      return;
    }

    if (this.itens.length !== 5) {
      alert('A cesta deve conter exatamente 5 ativos.');
      return;
    }

    if (this.totalPercentual !== 100) {
      alert('A soma dos percentuais deve ser exatamente 100%.');
      return;
    }

    const payload = {
      nome: this.form.value.nome,
      itens: this.itens.controls.map((control) => ({
        ticker: String(control.get('ticker')?.value || '').trim().toUpperCase(),
        percentual: Number(control.get('percentual')?.value || 0),
      })),
    };

    this.saving = true;

    this.cestasService.criarCesta(payload).subscribe({
      next: () => {
        this.saving = false;
        this.cdr.detectChanges();
        this.router.navigate(['/cestas']);
      },
      error: (err) => {
        console.error('Erro ao salvar cesta', err);
        this.saving = false;
        this.cdr.detectChanges();

        const mensagem =
          err?.error?.erro ||
          err?.error?.message ||
          'Não foi possível salvar a cesta. Verifique os dados e tente novamente.';

        alert(mensagem);
      },
    });
  }

  private criarItemForm(): FormGroup {
    return this.fb.group({
      ticker: ['', [Validators.required, Validators.minLength(4)]],
      percentual: [
        null,
        [Validators.required, Validators.min(1), Validators.max(100)],
      ],
    });
  }
}
