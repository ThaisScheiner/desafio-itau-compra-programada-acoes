import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ExecutarCompraResponse } from '../models/motor-compra.model';

@Injectable({
  providedIn: 'root',
})
export class MotorCompraService {
  private baseUrl = 'http://localhost:5005/api/motor';

  constructor(private http: HttpClient) {}

  executarCompra(dataReferencia: string): Observable<ExecutarCompraResponse> {
    return this.http.post<ExecutarCompraResponse>(`${this.baseUrl}/executar-compra`, {
      dataReferencia,
    });
  }
}