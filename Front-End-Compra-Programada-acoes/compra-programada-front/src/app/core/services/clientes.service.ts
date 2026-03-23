import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Cliente } from '../models/cliente.model';

export interface ClientesAtivosResponse {
  clientes: Cliente[];
}

export interface CarteiraAtivo {
  ticker: string;
  quantidade: number;
  precoMedio: number;
  cotacaoAtual: number;
  valorAtual: number;
  pl: number;
  plPercentual: number;
  composicaoCarteira: number;
}

export interface CarteiraClienteResponse {
  clienteId: number;
  nome: string;
  contaGrafica: string;
  dataConsulta: string;
  resumo: {
    valorTotalInvestido: number;
    valorAtualCarteira: number;
    plTotal: number;
    rentabilidadePercentual: number;
  };
  ativos: CarteiraAtivo[];
}

export interface HistoricoAporte {
  data: string;
  valor: number;
  parcela: string;
}

export interface EvolucaoCarteira {
  data: string;
  valorInvestido: number;
  valorCarteira: number;
  rentabilidade: number;
}

export interface RentabilidadeClienteResponse {
  clienteId: number;
  nome: string;
  dataConsulta: string;
  rentabilidade: {
    valorTotalInvestido: number;
    valorAtualCarteira: number;
    plTotal: number;
    rentabilidadePercentual: number;
  };
  historicoAportes: HistoricoAporte[];
  evolucaoCarteira: EvolucaoCarteira[];
}

@Injectable({
  providedIn: 'root',
})
export class ClientesService {
  private baseUrl = 'http://localhost:5001/api/clientes';

  constructor(private http: HttpClient) {}

  getClientesAtivos(): Observable<ClientesAtivosResponse> {
    return this.http.get<ClientesAtivosResponse>(`${this.baseUrl}/ativos`);
  }

  aderir(cliente: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/adesao`, cliente);
  }

  getCarteira(clienteId: number): Observable<CarteiraClienteResponse> {
    return this.http.get<CarteiraClienteResponse>(`${this.baseUrl}/${clienteId}/carteira`);
  }

  getRentabilidade(clienteId: number): Observable<RentabilidadeClienteResponse> {
    return this.http.get<RentabilidadeClienteResponse>(`${this.baseUrl}/${clienteId}/rentabilidade`);
  }

  atualizarValorMensal(clienteId: number, valorMensal: number): Observable<any> {
    return this.http.put(`${this.baseUrl}/${clienteId}/valor-mensal`, {
      valorMensal,
    });
  }

  encerrarCliente(clienteId: number): Observable<any> {
    return this.http.post(`${this.baseUrl}/${clienteId}/saida`, {});
  }
}