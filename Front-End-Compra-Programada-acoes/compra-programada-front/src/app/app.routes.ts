import { Routes } from '@angular/router';
import { ClientesComponent } from './features/clientes/pages/clientes/clientes.component';
import { ClienteFormComponent } from './features/clientes/pages/cliente-form/cliente-form.component';

import { CestasComponent } from './features/cestas/pages/cestas/cestas.component';
import { CestaFormComponent } from './features/cestas/pages/cestas-form/cestas-form.component';
import { MotorCompraComponent } from './features/motor-compra/pages/motor-compra/motor-compra.component';
import { ObservabilidadeComponent } from './features/observabilidade/pages/observabilidade/observabilidade.component';
import { DashboardComponent } from './features/dashboard/pages/dashboard/dashboard.component';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';
import { ClienteDetalheComponent } from './features/clientes/pages/cliente-detalhe/cliente-detalhe.component';


export const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    children: [
      { path: '', component: DashboardComponent },
      { path: 'clientes', component: ClientesComponent },
      { path: 'clientes/novo', component: ClienteFormComponent },
      { path: 'cestas', component: CestasComponent },
      { path: 'cestas/nova', component: CestaFormComponent },
      { path: 'motor-compra', component: MotorCompraComponent },
      { path: 'observabilidade', component: ObservabilidadeComponent },
      { path: 'clientes/:clienteId', component: ClienteDetalheComponent },
    ],
  },
];