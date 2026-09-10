import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { DocentesService } from '../../core/services/docentes.service';

@Component({
  selector: 'app-cancelar-turno',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './cancelar-turno.component.html'
})
export class CancelarTurnoComponent {
  private fb = inject(FormBuilder);
  private docentesService = inject(DocentesService);

  enviando = signal(false);
  error = signal<string | null>(null);
  cancelado = signal(false);

  form = this.fb.nonNullable.group({
    turnoId: ['', [Validators.required, Validators.pattern(/^\d+$/)]],
    telefono: ['', [Validators.required]]
  });

  cancelar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.enviando.set(true);
    this.error.set(null);
    const { turnoId, telefono } = this.form.getRawValue();

    this.docentesService.cancelarTurno(Number(turnoId), telefono).subscribe({
      next: () => {
        this.cancelado.set(true);
        this.enviando.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.enviando.set(false);
        if (err.status === 403) {
          this.error.set('El teléfono no coincide con el de la reserva.');
        } else if (err.status === 404) {
          this.error.set('No encontramos un turno reservado con ese número.');
        } else {
          this.error.set('No pudimos cancelar el turno. Intentá de nuevo en un momento.');
        }
      }
    });
  }
}
