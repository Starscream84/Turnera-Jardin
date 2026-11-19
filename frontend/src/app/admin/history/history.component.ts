import { Component, OnInit, computed, inject, signal, ChangeDetectionStrategy } from "@angular/core";
import { FormBuilder, ReactiveFormsModule } from "@angular/forms";
import { AuthService } from "../../core/services/auth.service";
import { AdminAppointmentsService, HistoryFilter, PAGE_SIZE } from "../../core/services/admin-appointments.service";
import { AdminTeachersService } from "../../core/services/admin-teachers.service";
import {
    APPOINTMENT_EVENT_LABELS,
    AppointmentEvent,
    AppointmentEventType,
    INTERVIEW_MODE_LABELS,
} from "../../core/models/appointment.model";
import { TeacherAdmin } from "../../core/models/teacher.model";
import { errorMessage } from "../../shared/http-error.util";

/**
 * Historial (H21): every booking, cancellation and confirmation request, newest first.
 * A cancelled appointment goes back to "Disponible" in Turnos; this screen is where the school can
 * still see who had booked it, who cancelled it and when. A teacher only sees her own.
 */
@Component({
    selector: "app-history",
    standalone: true,
    imports: [ReactiveFormsModule],
    changeDetection: ChangeDetectionStrategy.Eager,
    templateUrl: "./history.component.html",
})
export class HistoryComponent implements OnInit {
    private fb = inject(FormBuilder);
    private service = inject(AdminAppointmentsService);
    private teachersService = inject(AdminTeachersService);
    private auth = inject(AuthService);

    readonly eventLabels = APPOINTMENT_EVENT_LABELS;
    readonly modeLabels = INTERVIEW_MODE_LABELS;
    readonly pageSize = PAGE_SIZE;

    events = signal<AppointmentEvent[]>([]);
    teachers = signal<TeacherAdmin[]>([]);
    loading = signal(true);
    error = signal<string | null>(null);
    page = signal(1);
    total = signal(0);
    pageCount = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));

    isManagement = computed(() => this.auth.role() === "Admin" || this.auth.role() === "Coordinator");

    filterForm = this.fb.nonNullable.group({
        teacherId: [""],
        from: [""],
        to: [""],
        type: ["" as AppointmentEventType | ""],
    });

    private appliedFilter: HistoryFilter = {};

    ngOnInit(): void {
        if (this.isManagement()) {
            this.teachersService.list().subscribe({ next: (teachers) => this.teachers.set(teachers) });
        }
        this.search();
    }

    search(): void {
        const values = this.filterForm.getRawValue();
        this.appliedFilter = {
            teacherId: values.teacherId ? Number(values.teacherId) : undefined,
            from: values.from || undefined,
            to: values.to || undefined,
            type: values.type || undefined,
        };
        this.loadPage(1);
    }

    loadPage(page: number): void {
        this.loading.set(true);
        this.error.set(null);
        this.service.history(this.appliedFilter, page).subscribe({
            next: (result) => {
                this.events.set(result.items);
                this.total.set(result.total);
                this.page.set(page);
                this.loading.set(false);
            },
            error: (err: unknown) => {
                this.error.set(errorMessage(err, "No pudimos cargar el historial."));
                this.loading.set(false);
            },
        });
    }

    /** "2026-11-20" → "20/11/2026" */
    formatDate(date: string): string {
        const [y, m, d] = date.split("-");
        return `${d}/${m}/${y}`;
    }

    /** UTC timestamp → local date and time ("12/11 14:05"). */
    formatMoment(utc: string): string {
        const date = new Date(utc.endsWith("Z") ? utc : `${utc}Z`);
        return date.toLocaleString("es-AR", {
            day: "2-digit",
            month: "2-digit",
            hour: "2-digit",
            minute: "2-digit",
            timeZone: "America/Argentina/Buenos_Aires",
        });
    }

    badgeClass(type: AppointmentEventType): string {
        switch (type) {
            case "Booked":
                return "badge-booked";
            case "ConfirmationRequested":
                return "badge-available";
            default:
                return "badge-cancelled";
        }
    }
}
