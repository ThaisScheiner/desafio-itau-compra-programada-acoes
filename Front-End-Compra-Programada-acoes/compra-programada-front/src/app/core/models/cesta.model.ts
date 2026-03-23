export interface ItemCesta {
  ticker: string;
  percentual: number;
}

export interface Cesta {
  cestaId?: number;
  nome: string;
  ativa?: boolean;
  dataCriacao?: string;
  itens: ItemCesta[];
}