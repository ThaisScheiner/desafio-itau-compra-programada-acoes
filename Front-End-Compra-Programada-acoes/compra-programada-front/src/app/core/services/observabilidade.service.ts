import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class ObservabilidadeService {
  constructor(private http: HttpClient) {}

  private verificarServico(nome: string, url: string): Observable<any> {
    return this.http.get(url).pipe(
      map(() => ({
        nome,
        url,
        status: 'UP',
        httpStatus: 200,
        ultimaVerificacao: new Date().toLocaleString('pt-BR'),
        detalhes: 'Serviço respondeu com sucesso.'
      })),
      catchError((error) =>
        of({
          nome,
          url,
          status: 'DOWN',
          httpStatus: error.status || 'Erro',
          ultimaVerificacao: new Date().toLocaleString('pt-BR'),
          detalhes: 'Falha ao acessar o endpoint.'
        })
      )
    );
  }

  verificarClientes(): Observable<any> {
    return this.verificarServico('ClientesService', 'http://localhost:5001/health');
  }

  verificarCestas(): Observable<any> {
    return this.verificarServico('CestasRecomendacaoService', 'http://localhost:5002/health');
  }

  verificarCotacoes(): Observable<any> {
    return this.verificarServico('CotacoesService', 'http://localhost:5003/health');
  }

  verificarCustodias(): Observable<any> {
    return this.verificarServico('CustodiasService', 'http://localhost:5004/health');
  }

  verificarMotorCompra(): Observable<any> {
    return this.verificarServico('MotorCompraService', 'http://localhost:5005/health');
  }

  verificarEventosIr(): Observable<any> {
    return this.verificarServico('EventosIRService', 'http://localhost:5006/health');
  }

  verificarRebalanceamentos(): Observable<any> {
    return this.verificarServico('RebalanceamentosService', 'http://localhost:5007/health');
  }

  getResumoMotor(): Observable<any> {
    return this.http.get('http://localhost:5005/api/motor/ultimo-resumo');
  }
}