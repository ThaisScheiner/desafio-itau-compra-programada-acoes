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