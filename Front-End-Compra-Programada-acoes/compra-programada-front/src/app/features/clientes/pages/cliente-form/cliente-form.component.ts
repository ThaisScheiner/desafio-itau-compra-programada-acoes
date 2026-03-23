import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators, FormGroup } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { ClientesService } from '../../../../core/services/clientes.service';

@Component({
  selector: 'app-cliente-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule
  ],
  templateUrl: './cliente-form.component.html',
  styleUrl: './cliente-form.component.scss'
})
export class ClienteFormComponent {
  loading = false;
  erro = '';
  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    private clientesService: ClientesService,
    private router: Router
  ) {
    this.form = this.fb.group({
      nome: ['', [Validators.required, Validators.minLength(3)]],
      cpf: ['', [Validators.required, Validators.minLength(11), Validators.maxLength(11)]],
      email: ['', [Validators.required, Validators.email]],
      valorMensal: [null, [Validators.required, Validators.min(100)]]
    });
  }

  salvar(): void {
    this.erro = '';

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;

    this.clientesService.aderir(this.form.value).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate(['/clientes']);
      },
      error: (err) => {
        console.error('Erro ao cadastrar cliente', err);
        this.erro = err?.error?.erro || 'Não foi possível cadastrar o cliente.';
        this.loading = false;
      }
    });
  }

  get nome() {
    return this.form.get('nome');
  }

  get cpf() {
    return this.form.get('cpf');
  }

  get email() {
    return this.form.get('email');
  }

  get valorMensal() {
    return this.form.get('valorMensal');
  }
}