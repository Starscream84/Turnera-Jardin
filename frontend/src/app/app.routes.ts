import { Routes } from "@angular/router";
import { SelectTeacherComponent } from "./public-booking/select-teacher/select-teacher.component";
import { SelectAppointmentComponent } from "./public-booking/select-appointment/select-appointment.component";
import { BookingFormComponent } from "./public-booking/booking-form/booking-form.component";
import { CancelAppointmentComponent } from "./public-booking/cancel-appointment/cancel-appointment.component";
import { authGuard, passwordChangeGuard } from "./core/guards/auth.guard";
import { adminGuard, coordinatorGuard } from "./core/guards/admin.guard";

// The URL paths stay in Spanish on purpose: families and teachers see them and links were already shared.
// The public flow loads eagerly; the whole admin panel is lazy-loaded (loadComponent), so families
// don't download the panel's code on their phones.
export const routes: Routes = [
    { path: "", pathMatch: "full", redirectTo: "reservar" },

    // Public flow for families
    { path: "reservar", component: SelectTeacherComponent },
    { path: "reservar/:teacherId", component: SelectAppointmentComponent },
    { path: "reservar/:teacherId/turno/:appointmentId", component: BookingFormComponent },
    { path: "cancelar", component: CancelAppointmentComponent },

    // Admin panel
    {
        path: "admin/login",
        loadComponent: () => import("./admin/login/login.component").then((m) => m.LoginComponent),
    },
    {
        path: "admin",
        loadComponent: () =>
            import("./admin/layout/admin-layout.component").then((m) => m.AdminLayoutComponent),
        canActivate: [authGuard],
        canActivateChild: [passwordChangeGuard],
        children: [
            { path: "", pathMatch: "full", redirectTo: "turnos" },
            // Admin and Coordinator: teacher management and appointment generation.
            {
                path: "docentes",
                canActivate: [coordinatorGuard],
                loadComponent: () =>
                    import("./admin/teachers/teachers-list.component").then((m) => m.TeachersListComponent),
            },
            {
                path: "docentes/nuevo",
                canActivate: [coordinatorGuard],
                loadComponent: () =>
                    import("./admin/teachers/teacher-form.component").then((m) => m.TeacherFormComponent),
            },
            {
                path: "docentes/:id/editar",
                canActivate: [coordinatorGuard],
                loadComponent: () =>
                    import("./admin/teachers/teacher-form.component").then((m) => m.TeacherFormComponent),
            },
            {
                path: "turnos/generar",
                canActivate: [coordinatorGuard],
                loadComponent: () =>
                    import("./admin/appointments/generate-appointments.component").then(
                        (m) => m.GenerateAppointmentsComponent,
                    ),
            },
            // Admin only: user management.
            {
                path: "usuarios",
                canActivate: [adminGuard],
                loadComponent: () =>
                    import("./admin/users/users-list.component").then((m) => m.UsersListComponent),
            },
            // Available to management and teachers alike.
            {
                path: "historial",
                loadComponent: () =>
                    import("./admin/history/history.component").then((m) => m.HistoryComponent),
            },
            {
                path: "turnos",
                loadComponent: () =>
                    import("./admin/appointments/appointments-list.component").then(
                        (m) => m.AppointmentsListComponent,
                    ),
            },
            {
                path: "mi-cuenta",
                loadComponent: () =>
                    import("./admin/my-account/my-account.component").then((m) => m.MyAccountComponent),
            },
        ],
    },

    { path: "**", redirectTo: "reservar" },
];
