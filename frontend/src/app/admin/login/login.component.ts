import { Component, inject, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { AuthService } from "../../core/services/auth.service";
import { HttpErrorResponse } from "@angular/common/http";

@Component({
    selector: "app-login",
    standalone: true,
    imports: [ReactiveFormsModule, RouterLink],
    templateUrl: "./login.component.html",
})
export class LoginComponent {
    private fb = inject(FormBuilder);
    private auth = inject(AuthService);
    private router = inject(Router);

    sending = signal(false);
    error = signal<string | null>(null);

    form = this.fb.nonNullable.group({
        email: ["", [Validators.required, Validators.email]],
        password: ["", [Validators.required]],
    });

    signIn(): void {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        this.sending.set(true);
        this.error.set(null);

        this.auth.login(this.form.getRawValue()).subscribe({
            next: () => {
                this.sending.set(false);
                this.router.navigate(["/admin/turnos"]);
            },
            error: (err: HttpErrorResponse) => {
                this.sending.set(false);
                this.error.set(
                    err.status === 429
                        ? "Demasiados intentos de ingreso. Esperá unos minutos y probá de nuevo."
                        : "Email o contraseña incorrectos.",
                );
            },
        });
    }
}
