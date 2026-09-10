import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { DocentesService } from '../../core/services/docentes.service';
import { TurnoDisponible, TurnoConfirmado } from '../../core/models/turno.model';

@Component({
  selector: 'app-formulario-reserva',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './formulario-reserva.component.html'
})
export class FormularioReservaComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private docentesService = inject(DocentesService);
  private fb = inject(FormBuilder);

  docenteId!: number;
  turnoId!: number;
  turnoSeleccionado = signal<TurnoDisponible | null>(null);

  cargando = signal(true);
  enviando = signal(false);
  error = signal<string | null>(null);
  confirmacion = signal<TurnoConfirmado | null>(null);

  form = this.fb.nonNullable.group({
    nombrePadre: ['', [Validators.required, Validators.minLength(2)]],
    telefonoPadre: ['', [Validators.required, Validators.pattern(/^[0-9+\s-]{8,20}$/)]],
    nombreNino: ['', [Validators.required, Validators.minLength(2)]],
    observaciones: ['']
  });

  ngOnInit(): void {
    this.docenteId = Number(this.route.snapshot.paramMap.get('docenteId'));
    this.turnoId = Number(this.route.snapshot.paramMap.get('turnoId'));

    this.docentesService.turnosDisponibles(this.docenteId).subscribe({
      next: (turnos) => {
        const turno = turnos.find((t) => t.id === this.turnoId) ?? null;
        this.turnoSeleccionado.set(turno);
        if (!turno) {
          this.error.set('Este turno ya no está disponible. Volvé atrás y elegí otro horario.');
        }
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar los datos del turno.');
        this.cargando.set(false);
      }
    });
  }

  confirmar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.enviando.set(true);
    this.error.set(null);

    this.docentesService.reservarTurno(this.turnoId, this.form.getRawValue()).subscribe({
      next: (confirmado) => {
        this.confirmacion.set(confirmado);
        this.enviando.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.enviando.set(false);
        if (err.status === 409) {
          this.error.set('Justo se acaban de reservar este turno. Volvé atrás y elegí otro horario.');
        } else {
          this.error.set('No pudimos confirmar la reserva. Intentá de nuevo en un momento.');
        }
      }
    });
  }

  formatearFecha(fecha: string): string {
    const [anio, mes, dia] = fecha.split('-').map(Number);
    return new Date(anio, mes - 1, dia).toLocaleDateString('es-AR', { weekday: 'long', day: 'numeric', month: 'long' });
  }

  formatearHora(hora: string): string {
    return hora.slice(0, 5);
  }
}
