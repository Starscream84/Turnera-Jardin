import { Component, OnInit, computed, inject, signal, ChangeDetectionStrategy } from "@angular/core";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { BookingService } from "../../core/services/booking.service";
import {
    INTERVIEW_MODE_LABELS,
    InterviewMode,
    AvailableAppointment,
    BookedAppointment,
} from "../../core/models/appointment.model";
import { Teacher } from "../../core/models/teacher.model";
import { PublicShellComponent } from "../../shared/public-shell.component";
import { STRICT_EMAIL_PATTERN, phoneValidator } from "../../shared/phone.util";
import { fullNameValidator } from "../../shared/name.util";
import { errorCode, errorMessage } from "../../shared/http-error.util";

@Component({
    selector: "app-booking-form",
    standalone: true,
    imports: [ReactiveFormsModule, RouterLink, PublicShellComponent],
    changeDetection: ChangeDetectionStrategy.Eager,
    templateUrl: "./booking-form.component.html",
})
export class BookingFormComponent implements OnInit {
    private route = inject(ActivatedRoute);
    private bookingService = inject(BookingService);
    private fb = inject(FormBuilder);

    teacherId!: number;
    appointmentId!: number;
    selectedAppointment = signal<AvailableAppointment | null>(null);
    /** Chosen teacher, shown in the appointment summary. */
    teacher = signal<Teacher | null>(null);

    loading = signal(true);
    sending = signal(false);
    error = signal<string | null>(null);
    confirmation = signal<BookedAppointment | null>(null);

    /** Link that opens Google Calendar with the interview event already filled in, ready to save. */
    googleCalendarLink = computed(() => {
        const booked = this.confirmation();
        return booked ? this.buildGoogleCalendarLink(booked) : null;
    });

    form = this.fb.nonNullable.group({
        // Starts empty on purpose: the family says if it's the child's first interview
        // (first one = in person, the rest = online). Nothing is assumed.
        mode: ["" as InterviewMode | "", [Validators.required]],
        guardianName: ["", [Validators.required, Validators.maxLength(200), fullNameValidator]],
        guardianPhone: ["", [Validators.required, Validators.maxLength(30), phoneValidator]],
        guardianEmail: ["", [Validators.required, Validators.pattern(STRICT_EMAIL_PATTERN), Validators.maxLength(200)]],
        childName: ["", [Validators.required, Validators.maxLength(200), fullNameValidator]],
        notes: [""],
    });

    ngOnInit(): void {
        this.teacherId = Number(this.route.snapshot.paramMap.get("teacherId"));
        this.appointmentId = Number(this.route.snapshot.paramMap.get("appointmentId"));

        // Only to show the name in the summary: if it fails, booking keeps working the same.
        this.bookingService.listTeachers().subscribe({
            next: (teachers) => {
                const teacher = teachers.find((t) => t.id === this.teacherId) ?? null;
                this.teacher.set(teacher);
            },
        });

        // Only this slot (GET /appointments/{id}), not every free slot of the teacher.
        this.bookingService.getAppointment(this.appointmentId).subscribe({
            next: (appointment) => {
                if (appointment.teacherId !== this.teacherId) {
                    this.error.set("Este horario ya no está disponible. Volvé atrás y elegí otro.");
                } else {
                    this.selectedAppointment.set(appointment);
                }
                this.loading.set(false);
            },
            error: (err: unknown) => {
                this.error.set(
                    errorCode(err) === "APPOINTMENT_NOT_FOUND"
                        ? "Este horario ya no está disponible. Volvé atrás y elegí otro."
                        : errorMessage(err, "No pudimos cargar los datos de la entrevista."),
                );
                this.loading.set(false);
            },
        });
    }

    confirm(): void {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        this.sending.set(true);
        this.error.set(null);

        const values = this.form.getRawValue();
        const data = { ...values, mode: values.mode as InterviewMode };

        this.bookingService.bookAppointment(this.appointmentId, data).subscribe({
            next: (booked) => {
                this.confirmation.set(booked);
                this.sending.set(false);
            },
            error: (err: unknown) => {
                this.sending.set(false);
                // If the same family retries after a lost connection, the backend answers 200 with
                // the same booking, so a 409 here really means someone else took the slot.
                if (errorCode(err) === "APPOINTMENT_TAKEN") {
                    this.error.set(
                        "Otra familia acaba de reservar este horario. Volvé atrás y elegí otro.",
                    );
                } else {
                    // 400s carry the backend's reason (e.g. "solo atiende entrevistas presenciales").
                    this.error.set(
                        errorMessage(err, "No pudimos confirmar la reserva. Intentá de nuevo en un momento."),
                    );
                }
            },
        });
    }

    /** Spanish label of the mode, for the screen. Empty if nothing was chosen yet. */
    modeLabel(mode: InterviewMode | "" | null): string {
        return mode ? INTERVIEW_MODE_LABELS[mode] : "";
    }

    formatDate(date: string): string {
        const [year, month, day] = date.split("-").map(Number);
        return new Date(year, month - 1, day).toLocaleDateString("es-AR", {
            weekday: "long",
            day: "numeric",
            month: "long",
        });
    }

    formatTime(time: string): string {
        return time.slice(0, 5);
    }

    /**
     * Uses Google Calendar's public "create event" link: it needs no API or login on our side,
     * the event is saved to whatever Google account the family has open on their phone.
     */
    private buildGoogleCalendarLink(appointment: BookedAppointment): string {
        // Google expects "yyyyMMddTHHmmss"; the date arrives as "yyyy-MM-dd" and the time as "HH:mm:ss".
        const toGoogleFormat = (time: string) =>
            `${appointment.date.replaceAll("-", "")}T${time.replaceAll(":", "").padEnd(6, "0")}`;

        const params = new URLSearchParams({
            action: "TEMPLATE",
            text: `Entrevista en el jardín con ${appointment.teacherName}`,
            dates: `${toGoogleFormat(appointment.startTime)}/${toGoogleFormat(appointment.endTime)}`,
            // Without this Google uses the device's time zone, and the time would shift if the phone is in another zone.
            ctz: "America/Argentina/Buenos_Aires",
            details: `Entrevista ${this.modeLabel(appointment.mode).toLowerCase()} por ${appointment.childName}. Número de entrevista: #${appointment.id}.`,
        });
        // The backend only sends the address for in-person interviews.
        if (appointment.location) {
            params.set("location", appointment.location);
        }

        return `https://calendar.google.com/calendar/render?${params.toString()}`;
    }
}
