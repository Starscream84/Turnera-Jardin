import { Component, inject, signal } from "@angular/core";
import { RouterLink } from "@angular/router";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { HttpErrorResponse } from "@angular/common/http";
import { BookingService } from "../../core/services/booking.service";
import { PublicShellComponent } from "../../shared/public-shell.component";

@Component({
    selector: "app-cancel-appointment",
    standalone: true,
    imports: [ReactiveFormsModule, RouterLink, PublicShellComponent],
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
        phone: ["", [Validators.required]],
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
            error: (err: HttpErrorResponse) => {
                this.sending.set(false);
                if (err.status === 403) {
                    this.error.set("El teléfono no coincide con el de la reserva.");
                } else if (err.status === 404) {
                    this.error.set("No encontramos un turno reservado con ese número.");
                } else if (err.status === 429) {
                    this.error.set(
                        "Hiciste muchos intentos seguidos. Esperá unos minutos y probá de nuevo.",
                    );
                } else {
                    this.error.set("No pudimos cancelar el turno. Intentá de nuevo en un momento.");
                }
            },
        });
    }
}
