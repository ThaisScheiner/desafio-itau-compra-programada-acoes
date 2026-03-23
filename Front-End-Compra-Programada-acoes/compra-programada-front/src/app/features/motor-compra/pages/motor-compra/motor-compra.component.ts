import { ChangeDetectorRef, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MotorCompraService } from '../../../../core/services/motor-compra.service';
import { ExecutarCompraResponse } from '../../../../core/models/motor-compra.model';

@Component({
  selector: 'app-motor-compra',
  standalone: true,
  imports: [CommonModule, FormsModule, MatTableModule, MatButtonModule],
  templateUrl: './motor-compra.component.html',
  styleUrl: './motor-compra.component.scss',
})
export class MotorCompraComponent {
  loading = false;
  executando = false;

  dataReferencia = '';

  resultado: ExecutarCompraResponse | null = null;

  displayedOrdensColumns = ['ticker', 'quantidade', 'precoUnitario', 'tipoMercado'];
  displayedResiduosColumns = ['ticker', 'quantidade', 'precoMedio'];

  constructor(
    private motorCompraService: MotorCompraService,
    private cdr: ChangeDetectorRef
  ) {}

  executarCompra(): void {
    if (!this.dataReferencia) {
      alert('Informe a data de referência.');
      return;
    }

    this.executando = true;
    this.loading = true;
    this.resultado = null;

    this.motorCompraService.executarCompra(this.dataReferencia).subscribe({
      next: (response) => {
        this.resultado = response;
        this.executando = false;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Erro ao executar compra programada', err);
        this.executando = false;
        this.loading = false;
        this.cdr.detectChanges();

        const mensagem =
          err?.error?.erro ||
          err?.error?.message ||
          err?.error?.title ||
          'Não foi possível executar a compra programada.';

        alert(mensagem);
      },
    });
  }

  get totalOrdens(): number {
    return this.resultado?.ordensCompra?.length ?? 0;
  }

  get totalDistribuicoes(): number {
    return this.resultado?.distribuicoes?.length ?? 0;
  }

  get totalResiduos(): number {
    return this.resultado?.residuosCustodiaMaster?.length ?? 0;
  }
}