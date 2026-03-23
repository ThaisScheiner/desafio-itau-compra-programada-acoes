import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

interface AtalhoSistema {
  titulo: string;
  descricao: string;
  rota: string;
  tag: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent {
  atalhos: AtalhoSistema[] = [
    {
      titulo: 'Clientes',
      descricao: 'Gerencie adesão, listagem e manutenção dos clientes ativos do produto.',
      rota: '/clientes',
      tag: 'Cadastro',
    },
    {
      titulo: 'Cestas',
      descricao: 'Consulte a cesta ativa, histórico e cadastre novas composições Top Five.',
      rota: '/cestas',
      tag: 'Administração',
    },
    {
      titulo: 'Motor de Compra',
      descricao: 'Acompanhe a execução da compra programada e o fluxo operacional do motor.',
      rota: '/motor-compra',
      tag: 'Execução',
    },
    {
      titulo: 'Observabilidade',
      descricao: 'Monitore saúde dos microserviços, disponibilidade e dados operacionais.',
      rota: '/observabilidade',
      tag: 'Monitoramento',
    },
  ];
}