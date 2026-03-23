import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CriarCestaRequest {
  nome: string;
  itens: {
    ticker: string;
    percentual: number;
  }[];
}

@Injectable({
  providedIn: 'root',
})
export class CestasService {
  private baseUrl = 'http://localhost:5002/api/admin/cesta';

  constructor(private http: HttpClient) {}

  getCestaAtual(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/atual`);
  }

  getHistorico(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/historico`);
  }

  criarCesta(payload: CriarCestaRequest): Observable<any> {
    return this.http.post<any>(this.baseUrl, payload);
  }
}
