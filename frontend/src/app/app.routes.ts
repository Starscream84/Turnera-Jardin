import { Routes } from '@angular/router';
import { SeleccionarDocenteComponent } from './public-booking/seleccionar-docente/seleccionar-docente.component';
import { SeleccionarTurnoComponent } from './public-booking/seleccionar-turno/seleccionar-turno.component';
import { FormularioReservaComponent } from './public-booking/formulario-reserva/formulario-reserva.component';
import { CancelarTurnoComponent } from './public-booking/cancelar-turno/cancelar-turno.component';
import { LoginComponent } from './admin/login/login.component';
import { AdminLayoutComponent } from './admin/layout/admin-layout.component';
import { DocentesListComponent } from './admin/docentes/docentes-list.component';
import { DocenteFormComponent } from './admin/docentes/docente-form.component';
import { GenerarTurnosComponent } from './admin/turnos/generar-turnos.component';
import { TurnosListComponent } from './admin/turnos/turnos-list.component';
import { authGuard } from './core/guards/auth.guard';
import { PanelAgendaDocenteComponent } from './admin/panel-agenda-docente/panel-agenda-docente.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'reservar' },

  // Flujo público para las familias
  { path: 'reservar', component: SeleccionarDocenteComponent },
  { path: 'reservar/:docenteId', component: SeleccionarTurnoComponent },
  { path: 'reservar/:docenteId/turno/:turnoId', component: FormularioReservaComponent },
  { path: 'cancelar', component: CancelarTurnoComponent },

  // Ruta directa de desarrollo para ver tu componente sin pasar por el login
  { path: 'mi-agenda', component: PanelAgendaDocenteComponent },

  // Panel de administración
  { path: 'admin/login', component: LoginComponent },
  {
    path: 'admin',
    component: AdminLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'turnos' },
      { path: 'docentes', component: DocentesListComponent },
      { path: 'docentes/nuevo', component: DocenteFormComponent },
      { path: 'docentes/:id/editar', component: DocenteFormComponent },
      { path: 'turnos', component: TurnosListComponent },
      { path: 'turnos/generar', component: GenerarTurnosComponent },
      { path: 'agenda', component: PanelAgendaDocenteComponent }
    ]
  },

  { path: '**', redirectTo: 'reservar' }
];