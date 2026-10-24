import { Routes } from "@angular/router";
import { SeleccionarDocenteComponent } from "./public-booking/seleccionar-docente/seleccionar-docente.component";
import { SeleccionarTurnoComponent } from "./public-booking/seleccionar-turno/seleccionar-turno.component";
import { FormularioReservaComponent } from "./public-booking/formulario-reserva/formulario-reserva.component";
import { CancelarTurnoComponent } from "./public-booking/cancelar-turno/cancelar-turno.component";
import { LoginComponent } from "./admin/login/login.component";
import { AdminLayoutComponent } from "./admin/layout/admin-layout.component";
import { DocentesListComponent } from "./admin/docentes/docentes-list.component";
import { DocenteFormComponent } from "./admin/docentes/docente-form.component";
import { GenerarTurnosComponent } from "./admin/turnos/generar-turnos.component";
import { TurnosListComponent } from "./admin/turnos/turnos-list.component";
import { UsuariosListComponent } from "./admin/usuarios/usuarios-list.component";
import { MiCuentaComponent } from "./admin/mi-cuenta/mi-cuenta.component";
import { authGuard } from "./core/guards/auth.guard";
import { adminGuard, coordinadorGuard } from "./core/guards/admin.guard";

export const routes: Routes = [
    { path: "", pathMatch: "full", redirectTo: "reservar" },

    // Flujo público para las familias
    { path: "reservar", component: SeleccionarDocenteComponent },
    { path: "reservar/:docenteId", component: SeleccionarTurnoComponent },
    { path: "reservar/:docenteId/turno/:turnoId", component: FormularioReservaComponent },
    { path: "cancelar", component: CancelarTurnoComponent },

    // Panel de administración
    { path: "admin/login", component: LoginComponent },
    {
        path: "admin",
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            { path: "", pathMatch: "full", redirectTo: "turnos" },
            // Admin y Coordinador: gestión de docentes, generación de turnos y usuarios.
            { path: "docentes", component: DocentesListComponent, canActivate: [coordinadorGuard] },
            {
                path: "docentes/nuevo",
                component: DocenteFormComponent,
                canActivate: [coordinadorGuard],
            },
            {
                path: "docentes/:id/editar",
                component: DocenteFormComponent,
                canActivate: [coordinadorGuard],
            },
            {
                path: "turnos/generar",
                component: GenerarTurnosComponent,
                canActivate: [coordinadorGuard],
            },
            { path: "usuarios", component: UsuariosListComponent, canActivate: [adminGuard] },
            // Accesible para dirección y docentes por igual.
            { path: "turnos", component: TurnosListComponent },
            { path: "mi-cuenta", component: MiCuentaComponent },
        ],
    },

    { path: "**", redirectTo: "reservar" },
];
