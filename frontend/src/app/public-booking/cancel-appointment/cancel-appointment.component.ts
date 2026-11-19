import { Component, inject, signal, ChangeDetectionStrategy } from "@angular/core";
import { RouterLink } from "@angular/router";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { BookingService } from "../../core/services/booking.service";
import { PublicShellComponent } from "../../shared/public-shell.component";
import { errorMessage } from "../../shared/http-error.util";
import { phoneValidator } from "../../shared/phone.util";

@Component({
    selector: "app-cancel-appointment",
    standalone: true,
    imports: [ReactiveFormsModule, RouterLink, PublicShellComponent],
    changeDetection: ChangeDetectionStrategy.Eager,
    templateUrl: "./cancel-appointment.component.html",
})
export class CancelAppointmentComponent {
    private fb = inject(FormBuilder);
    private bookingService = inject(BookingService);

    sending = signal(false);
    error = signal<string | null>(null);
    cancelled = signal(false);

    form = this.fb.nonNullable.group({
        appointmentId: ["", [Validators.required, Validators.pattern(/^\d+$/)]],
        phone: ["", [Validators.required, phoneValidator]],
    });

    cancel(): void {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        this.sending.set(true);
        this.error.set(null);
        const { appointmentId, phone } = this.form.getRawValue();

        this.bookingService.cancelAppointment(Number(appointmentId), phone).subscribe({
            next: () => {
                this.cancelled.set(true);
                this.sending.set(false);
            },
            error: (err: unknown) => {
                this.sending.set(false);
                // The backend answers the same for "doesn't exist" and "phone doesn't match" (on purpose).
                this.error.set(
                    errorMessage(err, "No pudimos cancelar la entrevista. Intentá de nuevo en un momento."),
                );
            },
        });
    }
}
