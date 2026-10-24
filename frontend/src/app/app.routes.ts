import { Routes } from "@angular/router";
import { SelectTeacherComponent } from "./public-booking/select-teacher/select-teacher.component";
import { SelectAppointmentComponent } from "./public-booking/select-appointment/select-appointment.component";
import { BookingFormComponent } from "./public-booking/booking-form/booking-form.component";
import { CancelAppointmentComponent } from "./public-booking/cancel-appointment/cancel-appointment.component";
import { LoginComponent } from "./admin/login/login.component";
import { AdminLayoutComponent } from "./admin/layout/admin-layout.component";
import { TeachersListComponent } from "./admin/teachers/teachers-list.component";
import { TeacherFormComponent } from "./admin/teachers/teacher-form.component";
import { GenerateAppointmentsComponent } from "./admin/appointments/generate-appointments.component";
import { AppointmentsListComponent } from "./admin/appointments/appointments-list.component";
import { UsersListComponent } from "./admin/users/users-list.component";
import { MyAccountComponent } from "./admin/my-account/my-account.component";
import { authGuard } from "./core/guards/auth.guard";
import { adminGuard, coordinatorGuard } from "./core/guards/admin.guard";

// The URL paths stay in Spanish on purpose: families and teachers see them and links were already shared.
export const routes: Routes = [
    { path: "", pathMatch: "full", redirectTo: "reservar" },

    // Public flow for families
    { path: "reservar", component: SelectTeacherComponent },
    { path: "reservar/:teacherId", component: SelectAppointmentComponent },
    { path: "reservar/:teacherId/turno/:appointmentId", component: BookingFormComponent },
    { path: "cancelar", component: CancelAppointmentComponent },

    // Admin panel
    { path: "admin/login", component: LoginComponent },
    {
        path: "admin",
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            { path: "", pathMatch: "full", redirectTo: "turnos" },
            // Admin and Coordinator: teacher management and appointment generation.
            { path: "docentes", component: TeachersListComponent, canActivate: [coordinatorGuard] },
            {
                path: "docentes/nuevo",
                component: TeacherFormComponent,
                canActivate: [coordinatorGuard],
            },
            {
                path: "docentes/:id/editar",
                component: TeacherFormComponent,
                canActivate: [coordinatorGuard],
            },
            {
                path: "turnos/generar",
                component: GenerateAppointmentsComponent,
                canActivate: [coordinatorGuard],
            },
            // Admin only: user management.
            { path: "usuarios", component: UsersListComponent, canActivate: [adminGuard] },
            // Available to management and teachers alike.
            { path: "turnos", component: AppointmentsListComponent },
            { path: "mi-cuenta", component: MyAccountComponent },
        ],
    },

    { path: "**", redirectTo: "reservar" },
];
