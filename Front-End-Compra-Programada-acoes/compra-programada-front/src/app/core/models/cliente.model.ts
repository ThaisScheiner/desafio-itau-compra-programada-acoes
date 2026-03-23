export interface Cliente {
  clienteId: number;
  nome: string;
  cpf: string;
  email?: string;
  valorMensal: number;
  ativo?: boolean;
}