import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { DocentesService } from '../../core/services/docentes.service';
import { ModalidadEntrevista, TurnoDisponible, TurnoConfirmado } from '../../core/models/turno.model';

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

  /** Link que abre Google Calendar con el evento de la entrevista ya cargado, listo para guardar. */
  enlaceGoogleCalendar = computed(() => {
    const confirmado = this.confirmacion();
    return confirmado ? this.armarEnlaceGoogleCalendar(confirmado) : null;
  });

  form = this.fb.nonNullable.group({
    // Arranca vacío a propósito: la familia tiene que elegir, no se asume ninguna modalidad.
    modalidad: ['' as ModalidadEntrevista | '', [Validators.required]],
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

    const valores = this.form.getRawValue();
    const datos = { ...valores, modalidad: valores.modalidad as ModalidadEntrevista };

    this.docentesService.reservarTurno(this.turnoId, datos).subscribe({
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

  /**
   * Usa el link público "crear evento" de Google Calendar: no requiere API ni login nuestro,
   * el evento se guarda en la cuenta de Google que la familia tenga abierta en su teléfono.
   */
  private armarEnlaceGoogleCalendar(turno: TurnoConfirmado): string {
    // Google espera "yyyyMMddTHHmmss"; fecha llega como "yyyy-MM-dd" y la hora como "HH:mm:ss".
    const aFormatoGoogle = (hora: string) => `${turno.fecha.replaceAll('-', '')}T${hora.replaceAll(':', '').padEnd(6, '0')}`;

    const parametros = new URLSearchParams({
      action: 'TEMPLATE',
      text: `Entrevista en el jardín con ${turno.docenteNombre}`,
      dates: `${aFormatoGoogle(turno.horaInicio)}/${aFormatoGoogle(turno.horaFin)}`,
      // Sin esto Google toma la hora en la zona del dispositivo, y se correría si el teléfono está en otra zona.
      ctz: 'America/Argentina/Buenos_Aires',
      details: `Entrevista ${turno.modalidad.toLowerCase()} por ${turno.nombreNino}. Número de turno: #${turno.id}.`
    });

    return `https://calendar.google.com/calendar/render?${parametros.toString()}`;
  }
}
