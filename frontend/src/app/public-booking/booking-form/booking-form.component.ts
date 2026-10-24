import { Component, OnInit, computed, inject, signal } from "@angular/core";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { HttpErrorResponse } from "@angular/common/http";
import { BookingService } from "../../core/services/booking.service";
import {
    INTERVIEW_MODE_LABELS,
    InterviewMode,
    AvailableAppointment,
    BookedAppointment,
} from "../../core/models/appointment.model";
import { Teacher } from "../../core/models/teacher.model";
import { PublicShellComponent } from "../../shared/public-shell.component";

@Component({
    selector: "app-booking-form",
    standalone: true,
    imports: [ReactiveFormsModule, RouterLink, PublicShellComponent],
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

    /** First cycle and specialists don't offer in-person/online: the interview is always in person. */
    canChooseMode = computed(() => {
        const cycle = this.teacher()?.cycle;
        return cycle !== "FirstCycle" && cycle !== "Specialist";
    });

    /** Link that opens Google Calendar with the interview event already filled in, ready to save. */
    googleCalendarLink = computed(() => {
        const booked = this.confirmation();
        return booked ? this.buildGoogleCalendarLink(booked) : null;
    });

    form = this.fb.nonNullable.group({
        // Starts empty on purpose: the family has to choose, no mode is assumed.
        mode: ["" as InterviewMode | "", [Validators.required]],
        guardianFirstName: ["", [Validators.required, Validators.minLength(2)]],
        guardianLastName: ["", [Validators.required, Validators.minLength(2)]],
        guardianPhone: ["", [Validators.required, Validators.pattern(/^[0-9+\s-]{8,20}$/)]],
        guardianEmail: ["", [Validators.required, Validators.email, Validators.maxLength(200)]],
        childFirstName: ["", [Validators.required, Validators.minLength(2)]],
        childLastName: ["", [Validators.required, Validators.minLength(2)]],
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
                if (teacher?.cycle === "FirstCycle" || teacher?.cycle === "Specialist") {
                    // First cycle and specialists: the interview is always in person, the family doesn't choose.
                    this.form.controls.mode.setValue("InPerson");
                }
            },
        });

        this.bookingService.availableAppointments(this.teacherId).subscribe({
            next: (appointments) => {
                const appointment = appointments.find((a) => a.id === this.appointmentId) ?? null;
                this.selectedAppointment.set(appointment);
                if (!appointment) {
                    this.error.set(
                        "Este turno ya no está disponible. Volvé atrás y elegí otro horario.",
                    );
                }
                this.loading.set(false);
            },
            error: () => {
                this.error.set("No pudimos cargar los datos del turno.");
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
            error: (err: HttpErrorResponse) => {
                this.sending.set(false);
                if (err.status === 409) {
                    this.error.set(
                        "Justo se acaban de reservar este turno. Volvé atrás y elegí otro horario.",
                    );
                } else if (err.status === 429) {
                    this.error.set(
                        "Hiciste muchos intentos seguidos. Esperá unos minutos y probá de nuevo.",
                    );
                } else {
                    this.error.set(
                        "No pudimos confirmar la reserva. Intentá de nuevo en un momento.",
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
            details: `Entrevista ${this.modeLabel(appointment.mode).toLowerCase()} por ${appointment.childFirstName} ${appointment.childLastName}. Número de turno: #${appointment.id}.`,
        });

        return `https://calendar.google.com/calendar/render?${params.toString()}`;
    }
}
