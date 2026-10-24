import { Component, OnInit, computed, inject, signal } from "@angular/core";
import { RouterLink } from "@angular/router";
import { FormBuilder, ReactiveFormsModule } from "@angular/forms";
import { AdminAppointmentsService, AdminAppointmentsFilter } from "../../core/services/admin-appointments.service";
import { AdminTeachersService } from "../../core/services/admin-teachers.service";
import { AuthService } from "../../core/services/auth.service";
import {
    APPOINTMENT_STATUS_LABELS,
    INTERVIEW_MODE_LABELS,
    AppointmentAdmin,
    AppointmentStatus,
    InterviewMode,
} from "../../core/models/appointment.model";
import { TeacherAdmin } from "../../core/models/teacher.model";

@Component({
    selector: "app-appointments-list",
    standalone: true,
    imports: [ReactiveFormsModule, RouterLink],
    templateUrl: "./appointments-list.component.html",
})
export class AppointmentsListComponent implements OnInit {
    private fb = inject(FormBuilder);
    private service = inject(AdminAppointmentsService);
    private teachersService = inject(AdminTeachersService);
    auth = inject(AuthService);

    appointments = signal<AppointmentAdmin[]>([]);
    teachers = signal<TeacherAdmin[]>([]);
    loading = signal(true);
    error = signal<string | null>(null);
    notice = signal<string | null>(null);
    deleting = signal(false);
    printDate = "";

    /** Filter the list on screen was loaded with (not whatever is typed but not searched yet). */
    private appliedFilter: AdminAppointmentsFilter = {};

    /** Admin or Coordinator: sees every teacher's schedule and can generate and bulk-delete appointments. */
    isManagement = computed(() => this.auth.role() === "Admin" || this.auth.role() === "Coordinator");

    /** Totals of the list on screen (according to the applied filter), for the indicators at the top. */
    summary = computed(() => {
        const appointments = this.appointments();
        const count = (status: AppointmentStatus) => appointments.filter((a) => a.status === status).length;
        return {
            booked: count("Booked"),
            available: count("Available"),
            cancelled: count("Cancelled"),
            attendanceRequested: appointments.filter(
                (a) => a.status === "Booked" && a.confirmationRequestedAt,
            ).length,
        };
    });

    bookedForPrint = computed(() =>
        this.appointments()
            .filter((a) => a.status === "Booked")
            .sort((a, b) => (a.date + a.startTime).localeCompare(b.date + b.startTime)),
    );

    filterForm = this.fb.nonNullable.group({
        teacherId: [""],
        from: [""],
        to: [""],
        status: ["" as AppointmentStatus | ""],
    });

    ngOnInit(): void {
        if (this.isManagement()) {
            this.teachersService
                .list()
                .subscribe({ next: (teachers) => this.teachers.set(teachers) });
        }
        this.search();
    }

    search(): void {
        this.loading.set(true);
        this.error.set(null);
        const values = this.filterForm.getRawValue();
        const filter: AdminAppointmentsFilter = {
            teacherId: values.teacherId ? Number(values.teacherId) : undefined,
            from: values.from || undefined,
            to: values.to || undefined,
            status: values.status || undefined,
        };
        this.appliedFilter = filter;

        this.service.list(filter).subscribe({
            next: (appointments) => {
                this.appointments.set(appointments);
                this.loading.set(false);
            },
            error: () => {
                this.error.set("No pudimos cargar los turnos.");
                this.loading.set(false);
            },
        });
    }

    /**
     * Deletes every appointment of the list on screen (the ones the last "Buscar" brought).
     * The backend requires at least one filter.
     */
    deleteListed(): void {
        const total = this.appointments().length;
        if (total === 0) return;

        const booked = this.summary().booked;
        const warning = booked
            ? `\n\nATENCIÓN: ${booked} tienen una familia asignada. Se borran igual y NO se les avisa.`
            : "";
        const confirmed = confirm(
            `¿Eliminar los ${total} turnos de este listado?${warning}\n\nEsta acción no se puede deshacer.`,
        );
        if (!confirmed) return;

        this.deleting.set(true);
        this.error.set(null);
        this.notice.set(null);

        this.service.deleteMany(this.appliedFilter).subscribe({
            next: (response) => {
                this.deleting.set(false);
                this.notice.set(`Se eliminaron ${response.deleted} turno(s).`);
                this.search();
            },
            error: () => {
                this.deleting.set(false);
                this.error.set("No pudimos eliminar los turnos.");
            },
        });
    }

    cancel(appointment: AppointmentAdmin): void {
        const confirmed = confirm(
            `¿Cancelar el turno de ${appointment.childFirstName} ${appointment.childLastName} (responsable: ${appointment.guardianFirstName} ${appointment.guardianLastName})?`,
        );
        if (!confirmed) return;

        this.service.cancel(appointment.id).subscribe({
            next: () => this.search(),
            error: () => this.error.set("No pudimos cancelar el turno."),
        });
    }

    /**
     * Opens WhatsApp (the app on a phone, WhatsApp Web on a computer) with the message already written
     * for the family: it reminds them of the appointment and asks them to confirm. It is sent from the
     * WhatsApp of whoever presses the button, so it doesn't use Meta's API and costs nothing; the reply
     * arrives in that same chat.
     */
    requestConfirmation(appointment: AppointmentAdmin): void {
        const phone = this.phoneForWhatsApp(appointment.guardianPhone);
        if (!phone) {
            this.error.set("Este turno no tiene un WhatsApp válido cargado.");
            return;
        }

        // Opened within the same click: if it were opened after the server response,
        // the browser would block it as a pop-up.
        const url = `https://wa.me/${phone}?text=${encodeURIComponent(this.confirmationMessage(appointment))}`;
        window.open(url, "_blank", "noopener");

        this.error.set(null);
        this.service.requestConfirmation(appointment.id).subscribe({
            next: () => {
                const now = new Date().toISOString();
                this.appointments.update((appointments) =>
                    appointments.map((a) =>
                        a.id === appointment.id ? { ...a, confirmationRequestedAt: now } : a,
                    ),
                );
            },
            error: () =>
                this.error.set(
                    "Se abrió WhatsApp, pero no pudimos dejar registrado que se pidió la confirmación.",
                ),
        });
    }

    confirmationMessage(appointment: AppointmentAdmin): string {
        const [year, month, day] = appointment.date.split("-").map(Number);
        const date = new Date(year, month - 1, day).toLocaleDateString("es-AR", {
            weekday: "long",
            day: "numeric",
            month: "long",
        });
        const guardian = `${appointment.guardianFirstName ?? ""} ${appointment.guardianLastName ?? ""}`.trim();
        const greeting = guardian ? `Hola ${guardian}` : "Hola";
        const child = `${appointment.childFirstName ?? ""} ${appointment.childLastName ?? ""}`.trim() || "tu hijo/a";
        const mode = appointment.mode ? ` (${this.modeLabel(appointment.mode).toLowerCase()})` : "";

        return (
            `${greeting}, te escribimos del jardín para recordarte la entrevista por ${child} ` +
            `con ${appointment.teacherName}: ${date} a las ${this.formatTime(appointment.startTime)} hs${mode}.\n\n` +
            `¿Nos confirmás si vas a poder asistir? Respondé este mensaje con SÍ o NO.\n\n` +
            `Turno N.º ${appointment.id}. ¡Gracias!`
        );
    }

    /**
     * wa.me needs the full number with country code and without "+": for an Argentine mobile,
     * 549 + area code + number. Families enter it as "2235551234", so it is completed here.
     */
    phoneForWhatsApp(phone: string | null): string | null {
        let digits = (phone ?? "").replace(/\D/g, "");
        if (digits.startsWith("00")) digits = digits.slice(2);

        if (digits.startsWith("54")) {
            digits = digits.slice(2);
            if (digits.startsWith("9")) digits = digits.slice(1);
        }
        if (digits.startsWith("0")) digits = digits.slice(1);

        if (digits.length === 10) return `549${digits}`;
        // Any other length: it may be a number from abroad, it is used as entered.
        const original = (phone ?? "").replace(/\D/g, "");
        return original.length >= 8 ? original : null;
    }

    /** Opens the browser's print dialog. */
    print(): void {
        this.printDate = new Date().toLocaleDateString("es-AR");
        setTimeout(() => window.print());
    }

    /** Teacher name for the print header. */
    printTeacher(): string | null {
        if (!this.isManagement()) {
            return this.auth.userName();
        }
        const id = this.appliedFilter.teacherId;
        if (!id) return null;
        const teacher = this.teachers().find((t) => t.id === id);
        return teacher ? `${teacher.firstName} ${teacher.lastName}` : null;
    }

    /** Date range for the print header. */
    printRange(): string {
        const { from, to } = this.appliedFilter;
        if (from && to)
            return `Del ${this.formatDate(from)} al ${this.formatDate(to)}`;
        if (from) return `Desde el ${this.formatDate(from)}`;
        if (to) return `Hasta el ${this.formatDate(to)}`;
        return "Todas las fechas";
    }

    /** "03/10 08:40" in local time. The backend stores UTC and returns it without the "Z". */
    formatDateTime(iso: string): string {
        const date = new Date(/Z|[+-]\d\d:\d\d$/.test(iso) ? iso : `${iso}Z`);
        return date.toLocaleString("es-AR", {
            day: "2-digit",
            month: "2-digit",
            hour: "2-digit",
            minute: "2-digit",
        });
    }

    delete(appointment: AppointmentAdmin): void {
        const confirmed = confirm("¿Eliminar esta franja horaria disponible?");
        if (!confirmed) return;

        this.service.delete(appointment.id).subscribe({
            next: () => this.search(),
            error: () => this.error.set("No pudimos eliminar el turno."),
        });
    }

    formatDate(date: string): string {
        const [year, month, day] = date.split("-").map(Number);
        return new Date(year, month - 1, day).toLocaleDateString("es-AR");
    }

    formatTime(time: string): string {
        return time.slice(0, 5);
    }

    statusLabel(status: AppointmentStatus): string {
        return APPOINTMENT_STATUS_LABELS[status];
    }

    modeLabel(mode: InterviewMode | null): string {
        return mode ? INTERVIEW_MODE_LABELS[mode] : "";
    }

    badgeClass(status: AppointmentStatus): string {
        return {
            Available: "badge-available",
            Booked: "badge-booked",
            Cancelled: "badge-cancelled",
            Completed: "badge-completed",
        }[status];
    }
}
