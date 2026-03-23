import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { ClientesService } from '../../../../core/services/clientes.service';
import { Cliente } from '../../../../core/models/cliente.model';

@Component({
  selector: 'app-clientes',
  standalone: true,
  imports: [CommonModule, RouterLink, MatTableModule, MatButtonModule],
  templateUrl: './clientes.component.html',
  styleUrl: './clientes.component.scss',
})
export class ClientesComponent implements OnInit {
  clientes: Cliente[] = [];
  loading = false;

  displayedColumns = ['clienteId', 'nome', 'cpf', 'valorMensal', 'acoes'];

  constructor(
    private clientesService: ClientesService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadClientes();
  }

  get totalAporteMensal(): number {
    return this.clientes.reduce(
      (total, cliente) => total + cliente.valorMensal,
      0
    );
  }

  loadClientes(): void {
    this.loading = true;

    this.clientesService.getClientesAtivos().subscribe({
      next: (data) => {
        this.clientes = data.clientes;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Erro ao carregar clientes', err);
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }
}