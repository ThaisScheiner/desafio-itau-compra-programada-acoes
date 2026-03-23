export interface PlanoPorTicker {
  ticker: string;
  qtdDesejada: number;
  saldoMasterAnterior: number;
  qtdComprada: number;
  totalDisponivel: number;
}

export interface OrdemCompra {
  id: number;
  ticker: string;
  quantidade: number;
  precoUnitario: number;
  tipoMercado: string;
}

export interface DistribuicaoAtivo {
  ticker: string;
  quantidade: number;
}

export interface DistribuicaoCliente {
  clienteId: number;
  valorAporte: number;
  ativos: DistribuicaoAtivo[];
}

export interface ResiduoCustodiaMaster {
  ticker: string;
  quantidade: number;
  precoMedio: number;
}

export interface ExecutarCompraResponse {
  dataExecucao: string;
  dataReferencia: string;
  totalClientes: number;
  totalConsolidado: number;
  planoPorTicker: PlanoPorTicker[];
  ordensCompra: OrdemCompra[];
  distribuicoes: DistribuicaoCliente[];
  eventosIRPublicados: number;
  movimentacoesCustodia: number;
  residuosCustodiaMaster: ResiduoCustodiaMaster[];
  mensagem: string;
}