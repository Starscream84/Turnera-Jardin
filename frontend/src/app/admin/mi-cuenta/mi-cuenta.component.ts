import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { iniciales } from '../../shared/avatar.util';

const TAMANO_MAXIMO_BYTES = 2 * 1024 * 1024; // 2 MB, debe coincidir con el límite del backend
const TIPOS_VALIDOS = ['image/jpeg', 'image/png', 'image/webp'];

/** Pantalla para que cualquier usuario logueado (dirección o docente) cambie su propia contraseña. */
@Component({
  selector: 'app-mi-cuenta',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './mi-cuenta.component.html'
})
export class MiCuentaComponent {
  private fb = inject(FormBuilder);
  auth = inject(AuthService);

  readonly iniciales = iniciales;

  guardando = signal(false);
  error = signal<string | null>(null);
  exito = signal(false);

  subiendoFoto = signal(false);
  errorFoto = signal<string | null>(null);

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

  seleccionarFoto(evento: Event): void {
    const input = evento.target as HTMLInputElement;
    const archivo = input.files?.[0];
    if (!archivo) return;

    this.errorFoto.set(null);

    if (!TIPOS_VALIDOS.includes(archivo.type)) {
      this.errorFoto.set('Solo se aceptan imágenes JPG, PNG o WEBP.');
      input.value = '';
      return;
    }

    if (archivo.size > TAMANO_MAXIMO_BYTES) {
      this.errorFoto.set('La imagen no puede superar los 2 MB.');
      input.value = '';
      return;
    }

    this.subiendoFoto.set(true);
    this.auth.subirFoto(archivo).subscribe({
      next: () => {
        this.subiendoFoto.set(false);
        input.value = '';
      },
      error: (err) => {
        this.subiendoFoto.set(false);
        this.errorFoto.set(err?.error?.mensaje ?? 'No pudimos subir la foto.');
        input.value = '';
      }
    });
  }
}
