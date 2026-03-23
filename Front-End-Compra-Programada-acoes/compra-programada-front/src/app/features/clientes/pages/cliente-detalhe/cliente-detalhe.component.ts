import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { Cliente } from '../../../../core/models/cliente.model';
import {
  CarteiraAtivo,
  CarteiraClienteResponse,
  ClientesService,
  RentabilidadeClienteResponse,
} from '../../../../core/services/clientes.service';

@Component({
  selector: 'app-cliente-detalhe',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatTableModule],
  templateUrl: './cliente-detalhe.component.html',
  styleUrl: './cliente-detalhe.component.scss',
})
export class ClienteDetalheComponent implements OnInit {
  clienteId = 0;
  cliente?: Cliente;

  carteira: CarteiraAtivo[] = [];
  carteiraResponse?: CarteiraClienteResponse;
  rentabilidadeResponse?: RentabilidadeClienteResponse;

  loading = true;
  erro = '';

  displayedColumns = [
    'ticker',
    'quantidade',
    'precoMedio',
    'cotacaoAtual',
    'valorAtual',
    'composicaoCarteira',
    'pl',
    'plPercentual',
  ];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private clientesService: ClientesService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('clienteId');
    console.log('clienteId da rota:', idParam);

    const id = Number(idParam);

    if (!id) {
      this.erro = 'Cliente inválido.';
      this.loading = false;
      this.cdr.detectChanges();
      return;
    }

    this.clienteId = id;
    this.carregarDados();
  }

  carregarDados(): void {
    this.loading = true;
    this.erro = '';
    this.cdr.detectChanges();

    forkJoin({
      lista: this.clientesService.getClientesAtivos().pipe(
        catchError((err) => {
          console.error('Erro ao buscar lista de clientes:', err);
          return of({ clientes: [] as Cliente[] });
        })
      ),
      carteira: this.clientesService.getCarteira(this.clienteId).pipe(
        catchError((err) => {
          console.error('Erro ao buscar carteira:', err);
          return of(undefined);
        })
      ),
      rentabilidade: this.clientesService.getRentabilidade(this.clienteId).pipe(
        catchError((err) => {
          console.error('Erro ao buscar rentabilidade:', err);
          return of(undefined);
        })
      ),
    }).subscribe({
      next: ({ lista, carteira, rentabilidade }) => {
        console.log('Lista clientes:', lista);
        console.log('Carteira response:', carteira);
        console.log('Rentabilidade response:', rentabilidade);

        this.cliente = lista.clientes.find(
          (c) => c.clienteId === this.clienteId
        );

        this.carteiraResponse = carteira;
        this.rentabilidadeResponse = rentabilidade;
        this.carteira = carteira?.ativos ?? [];

        if (!this.cliente && carteira) {
          this.cliente = {
            clienteId: carteira.clienteId,
            nome: carteira.nome,
            cpf: '',
            email: '',
            valorMensal: 0,
            ativo: true,
          };
        }

        if (!this.cliente) {
          this.erro = 'Cliente não encontrado.';
        }

        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Erro ao carregar detalhe do cliente:', err);
        this.erro = 'Não foi possível carregar os detalhes do cliente.';
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  get quantidadeAtivos(): number {
    return this.carteira.length;
  }

  get statusCliente(): string {
    return this.cliente?.ativo === false ? 'Inativo' : 'Ativo';
  }

  get valorMensal(): number {
    return this.cliente?.valorMensal ?? 0;
  }

  get valorInvestido(): number {
    return this.rentabilidadeResponse?.rentabilidade?.valorTotalInvestido ?? 0;
  }

  get valorAtualCarteira(): number {
    return this.rentabilidadeResponse?.rentabilidade?.valorAtualCarteira ?? 0;
  }

  get plTotal(): number {
    return this.rentabilidadeResponse?.rentabilidade?.plTotal ?? 0;
  }

  get rentabilidadePercentual(): number {
    return this.rentabilidadeResponse?.rentabilidade?.rentabilidadePercentual ?? 0;
  }

  get contaGrafica(): string {
    return this.carteiraResponse?.contaGrafica ?? '-';
  }

  get dataConsulta(): string {
    return this.carteiraResponse?.dataConsulta ?? '';
  }
}