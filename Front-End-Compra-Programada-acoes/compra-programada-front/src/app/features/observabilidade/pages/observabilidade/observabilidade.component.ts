import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { forkJoin, of, Subject, timer } from 'rxjs';
import { catchError, map, takeUntil } from 'rxjs/operators';

interface StatusServico {
  nome: string;
  url: string;
  status: 'UP' | 'DOWN';
  httpStatus: number | string;
  ultimaVerificacao: string;
  detalhes: string;
  traceId: string;
  spanId: string;
  correlationId: string;
  tempoRespostaMs: number;
}

@Component({
  selector: 'app-observabilidade',
  standalone: true,
  imports: [CommonModule, MatButtonModule],
  templateUrl: './observabilidade.component.html',
  styleUrl: './observabilidade.component.scss',
})
export class ObservabilidadeComponent implements OnInit, OnDestroy {
  loading = false;

  servicos: StatusServico[] = [];

  totalUp = 0;
  totalDown = 0;
  statusGeral = 'Carregando...';

  // métricas de negócio (por enquanto fixas / já conhecidas do seu projeto)
  clientesProcessados = 4;
  totalConsolidado = 4533.33;
  eventosIr = 20;
  movimentacoesCustodia = 45;

  ultimaAtualizacao = '';

  private destroy$ = new Subject<void>();

  constructor(
    private http: HttpClient,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.carregarStatus();

    timer(10000, 10000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.carregarStatus(false);
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  carregarStatus(exibirLoading = true): void {
    if (exibirLoading) {
      this.loading = true;
    }

    const agora = new Date();
    const agoraFormatado = agora.toLocaleString('pt-BR');

    const endpoints = [
      { nome: 'ClientesService', url: 'http://localhost:5001/health' },
      { nome: 'CestasRecomendacaoService', url: 'http://localhost:5002/health' },
      { nome: 'CotacoesService', url: 'http://localhost:5003/health' },
      { nome: 'CustodiasService', url: 'http://localhost:5004/health' },
      { nome: 'MotorCompraService', url: 'http://localhost:5005/health' },
      { nome: 'EventosIRService', url: 'http://localhost:5006/health' },
      { nome: 'RebalanceamentosService', url: 'http://localhost:5007/health' },
    ];

    const requisicoes = endpoints.map((servico) => {
      const inicio = performance.now();

      return this.http.get(servico.url, {
        observe: 'response',
        responseType: 'text',
      }).pipe(
        map((response) => {
          const fim = performance.now();

          return {
            nome: servico.nome,
            url: servico.url,
            status: 'UP' as const,
            httpStatus: response.status,
            ultimaVerificacao: agoraFormatado,
            detalhes: 'Serviço disponível',
            traceId: response.headers.get('X-Trace-Id') ?? '-',
            spanId: response.headers.get('X-Span-Id') ?? '-',
            correlationId: response.headers.get('X-Correlation-Id') ?? '-',
            tempoRespostaMs: Math.round(fim - inicio),
          };
        }),
        catchError((error) => {
          const fim = performance.now();

          return of({
            nome: servico.nome,
            url: servico.url,
            status: 'DOWN' as const,
            httpStatus: error?.status || 'Sem resposta',
            ultimaVerificacao: agoraFormatado,
            detalhes: error?.message || 'Falha ao consultar serviço',
            traceId: '-',
            spanId: '-',
            correlationId: '-',
            tempoRespostaMs: Math.round(fim - inicio),
          });
        })
      );
    });

    forkJoin(requisicoes).subscribe({
      next: (resultado) => {
        this.servicos = resultado.sort((a, b) => a.nome.localeCompare(b.nome));
        this.recalcularResumo();
        this.ultimaAtualizacao = agoraFormatado;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Erro geral ao carregar observabilidade', err);
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  private recalcularResumo(): void {
    this.totalUp = this.servicos.filter((s) => s.status === 'UP').length;
    this.totalDown = this.servicos.filter((s) => s.status === 'DOWN').length;

    if (this.totalDown === 0) {
      this.statusGeral = 'Todos os serviços operacionais';
      return;
    }

    if (this.totalUp === 0) {
      this.statusGeral = 'Todos os serviços indisponíveis';
      return;
    }

    this.statusGeral = 'Há serviços indisponíveis';
  }
}