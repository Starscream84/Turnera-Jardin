import { Component, inject, signal, ChangeDetectionStrategy } from "@angular/core";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { AuthService } from "../../core/services/auth.service";
import { HttpErrorResponse } from "@angular/common/http";
import { errorMessage } from "../../shared/http-error.util";

@Component({
    selector: "app-login",
    standalone: true,
    imports: [ReactiveFormsModule, RouterLink],
    changeDetection: ChangeDetectionStrategy.Eager,
    templateUrl: "./login.component.html",
})
export class LoginComponent {
    private fb = inject(FormBuilder);
    private auth = inject(AuthService);
    private router = inject(Router);
    private route = inject(ActivatedRoute);

    sending = signal(false);
    error = signal<string | null>(null);
    /** Shown when the interceptor sent the user here because the session expired or was revoked. */
    sessionExpired = this.route.snapshot.queryParamMap.get("sesion") === "vencida";

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
                    err.status === 401
                        ? "Email o contraseña incorrectos."
                        : errorMessage(err, "No pudimos iniciar sesión. Intentá de nuevo en un momento."),
                );
            },
        });
    }
}
