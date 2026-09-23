import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminTurnosService } from '../../core/services/admin-turnos.service';
import { TurnoAdmin } from '../../core/models/turno.model';

@Component({
  selector: 'app-panel-agenda-docente',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './panel-agenda-docente.component.html',
  styleUrls: ['./panel-agenda-docente.component.css']
})
export class PanelAgendaDocenteComponent implements OnInit {
  @ViewChild('datePicker') datePicker!: ElementRef<HTMLInputElement>;

  docenteNombre = 'Lucas Acosta';
  salaAsignada = 'Sala Roja (4 años)';
  docenteId = 1;

  citas: TurnoAdmin[] = [];
  vistaActual: 'dia' | 'semana' = 'dia';
  fechaSeleccionada: Date = new Date();

  citaSeleccionada: TurnoAdmin | null = null;
  textoObservacion = '';
  mostrarModalConfig = false;
  cargando = false;

  constructor(private turnosService: AdminTurnosService) {}

  ngOnInit(): void {
    this.cargarTurnos();
  }

  cargarTurnos(): void {
    this.cargando = true;
    const fechaIso = this.fechaSeleccionada.toISOString().split('T')[0];

    this.turnosService.listar({
      docenteId: this.docenteId,
      desde: fechaIso,
      hasta: fechaIso
    }).subscribe({
      next: (turnos) => {
        if (turnos && turnos.length > 0) {
          this.citas = turnos;
          if (turnos[0].docenteNombre) {
            this.docenteNombre = turnos[0].docenteNombre;
          }
        } else {
          this.cargarTurnosMock();
        }
        this.cargando = false;
      },
      error: () => {
        this.cargarTurnosMock();
        this.cargando = false;
      }
    });
  }

  cargarTurnosMock(): void {
    this.citas = [
      {
        id: 101,
        docenteId: 1,
        docenteNombre: 'Lucas Acosta',
        fecha: this.fechaSeleccionada.toISOString().split('T')[0],
        horaInicio: '08:30:00',
        horaFin: '09:00:00',
        estado: 'Reservado',
        nombrePadre: 'Mariana Gómez',
        telefonoPadre: '223-519-9012',
        nombreNino: 'Mateo Benítez',
        observaciones: 'Traer ficha médica y certificado de vacunas.',
        confirmacionEnviada: true,
        recordatorioEnviado: false
      },
      {
        id: 102,
        docenteId: 1,
        docenteNombre: 'Lucas Acosta',
        fecha: this.fechaSeleccionada.toISOString().split('T')[0],
        horaInicio: '09:15:00',
        horaFin: '09:45:00',
        estado: 'Disponible',
        nombrePadre: 'Carlos Fernández',
        telefonoPadre: '223-495-1182',
        nombreNino: 'Sofía Fernández',
        observaciones: '',
        confirmacionEnviada: false,
        recordatorioEnviado: false
      },
      {
        id: 103,
        docenteId: 1,
        docenteNombre: 'Lucas Acosta',
        fecha: this.fechaSeleccionada.toISOString().split('T')[0],
        horaInicio: '10:00:00',
        horaFin: '10:30:00',
        estado: 'Completado',
        nombrePadre: 'Laura Pérez',
        telefonoPadre: '223-506-5984',
        nombreNino: 'Joaquín Díaz',
        observaciones: 'Entrevista realizada con ambos tutores. Excelente adaptación prevista.',
        confirmacionEnviada: true,
        recordatorioEnviado: true
      }
    ];
  }

  contarPorEstado(estado: string): number {
    return this.citas.filter(c => c.estado?.toLowerCase() === estado.toLowerCase()).length;
  }

  cambiarDia(dias: number): void {
    const nuevaFecha = new Date(this.fechaSeleccionada);
    nuevaFecha.setDate(nuevaFecha.getDate() + dias);
    this.fechaSeleccionada = nuevaFecha;
    this.cargarTurnos();
  }

  get fechaFormateada(): string {
    return this.fechaSeleccionada.toLocaleDateString('es-AR', {
      weekday: 'long',
      day: 'numeric',
      month: 'long'
    });
  }

  get fechaEnFormatoInput(): string {
    const anio = this.fechaSeleccionada.getFullYear();
    const mes = String(this.fechaSeleccionada.getMonth() + 1).padStart(2, '0');
    const dia = String(this.fechaSeleccionada.getDate()).padStart(2, '0');
    return `${anio}-${mes}-${dia}`;
  }

  abrirCalendario(): void {
    if (this.datePicker?.nativeElement) {
      if ('showPicker' in HTMLInputElement.prototype) {
        this.datePicker.nativeElement.showPicker();
      } else {
        this.datePicker.nativeElement.focus();
      }
    }
  }

  seleccionarFechaDirecta(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.value) {
      const [anio, mes, dia] = input.value.split('-').map(Number);
      this.fechaSeleccionada = new Date(anio, mes - 1, dia);
      this.cargarTurnos();
    }
  }

  formatearHora(hora: string | null): string {
    if (!hora) return '--:--';
    return hora.substring(0, 5);
  }

  abrirModalObservacion(cita: TurnoAdmin): void {
    this.citaSeleccionada = cita;
    this.textoObservacion = cita.observaciones || '';
  }

  cerrarModal(): void {
    this.citaSeleccionada = null;
    this.textoObservacion = '';
  }

  guardarObservacion(): void {
    if (!this.citaSeleccionada) return;
    const id = this.citaSeleccionada.id;
    const nuevaNota = this.textoObservacion;

    this.turnosService.actualizarObservacion(id, nuevaNota).subscribe({
      next: () => {
        if (this.citaSeleccionada) {
          this.citaSeleccionada.observaciones = nuevaNota;
        }
        this.cerrarModal();
      },
      error: () => {
        if (this.citaSeleccionada) {
          this.citaSeleccionada.observaciones = nuevaNota;
        }
        this.cerrarModal();
      }
    });
  }

  abrirModalConfig(): void {
    this.mostrarModalConfig = true;
  }
}