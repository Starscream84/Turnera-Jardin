import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';

/** Pantalla para que cualquier usuario logueado (dirección o docente) cambie su propia contraseña. */
@Component({
  selector: 'app-mi-cuenta',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './mi-cuenta.component.html'
})
export class MiCuentaComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);

  guardando = signal(false);
  error = signal<string | null>(null);
  exito = signal(false);

  form = this.fb.nonNullable.group({
    passwordActual: ['', Validators.required],
    passwordNueva: ['', [Validators.required, Validators.minLength(8)]],
    confirmarPassword: ['', Validators.required]
  });

  cambiar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const valores = this.form.getRawValue();
    if (valores.passwordNueva !== valores.confirmarPassword) {
      this.error.set('Las dos contraseñas nuevas no coinciden.');
      return;
    }

    this.guardando.set(true);
    this.error.set(null);
    this.exito.set(false);

    this.auth
      .cambiarPassword({ passwordActual: valores.passwordActual, passwordNueva: valores.passwordNueva })
      .subscribe({
        next: () => {
          this.guardando.set(false);
          this.exito.set(true);
          this.form.reset();
        },
        error: (err) => {
          this.guardando.set(false);
          this.error.set(err?.error?.mensaje ?? 'No pudimos cambiar la contraseña.');
        }
      });
  }
}
