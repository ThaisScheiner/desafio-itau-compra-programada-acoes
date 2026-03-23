import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { CestasService } from '../../../../core/services/cestas.service';

export interface ItemCesta {
  ticker: string;
  percentual: number;
}

export interface Cesta {
  cestaId: number;
  nome: string;
  ativa: boolean;
  dataCriacao: string;
  itens: ItemCesta[];
}

@Component({
  selector: 'app-cestas',
  standalone: true,
  imports: [CommonModule, RouterLink, MatTableModule, DatePipe],
  templateUrl: './cestas.component.html',
  styleUrl: './cestas.component.scss',
})
export class CestasComponent implements OnInit {
  loading = false;

  cestaAtual: Cesta | null = null;
  historico: Cesta[] = [];
  cestas: Cesta[] = [];

  displayedColumns = ['cestaId', 'nome', 'status', 'dataCriacao'];

  constructor(
    private cestasService: CestasService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.carregarDados();
  }

  carregarDados(): void {
    this.loading = true;

    this.cestasService.getCestaAtual().subscribe({
      next: (cestaAtualResponse) => {
        this.cestaAtual = cestaAtualResponse;

        this.cestasService.getHistorico().subscribe({
          next: (historicoResponse) => {
            this.historico = this.normalizarHistorico(historicoResponse);
            this.montarListaPrincipal();

            this.loading = false;
            this.cdr.detectChanges();
          },
          error: (err) => {
            console.error('Erro ao carregar histórico de cestas', err);

            this.historico = this.cestaAtual ? [this.cestaAtual] : [];
            this.montarListaPrincipal();

            this.loading = false;
            this.cdr.detectChanges();
          },
        });
      },
      error: (err) => {
        console.error('Erro ao carregar cesta atual', err);

        this.cestaAtual = null;

        this.cestasService.getHistorico().subscribe({
          next: (historicoResponse) => {
            this.historico = this.normalizarHistorico(historicoResponse);
            this.montarListaPrincipal();

            this.loading = false;
            this.cdr.detectChanges();
          },
          error: (historicoErr) => {
            console.error('Erro ao carregar histórico de cestas', historicoErr);

            this.historico = [];
            this.cestas = [];
            this.loading = false;
            this.cdr.detectChanges();
          },
        });
      },
    });
  }

  private montarListaPrincipal(): void {
    if (this.historico.length > 0) {
      this.cestas = [...this.historico];
      return;
    }

    this.cestas = this.cestaAtual ? [this.cestaAtual] : [];
  }

  private normalizarHistorico(response: any): Cesta[] {
    if (!response) {
      return [];
    }

    if (Array.isArray(response)) {
      return response;
    }

    if (Array.isArray(response.cestas)) {
      return response.cestas;
    }

    return [];
  }
}
