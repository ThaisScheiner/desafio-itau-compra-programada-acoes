import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class ShellComponent {
  menuItems = [
    { label: 'Dashboard', route: '/dashboard', icon: 'dashboard' },
    { label: 'Clientes', route: '/clientes', icon: 'group' },
    { label: 'Cestas', route: '/cestas', icon: 'pie_chart' },
    { label: 'Cotações', route: '/cotacoes', icon: 'show_chart' },
    { label: 'Motor Compra', route: '/motor-compra', icon: 'play_circle' },
    { label: 'Custódias', route: '/custodias', icon: 'account_balance_wallet' },
    { label: 'Eventos IR', route: '/eventos-ir', icon: 'receipt_long' },
    { label: 'Rebalanceamentos', route: '/rebalanceamentos', icon: 'sync_alt' },
    { label: 'Observabilidade', route: '/observabilidade', icon: 'monitoring' },
  ];
}