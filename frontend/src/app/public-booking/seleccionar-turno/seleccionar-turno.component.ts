// NUEVO: se suman `computed` (para las horas del día elegido) y `forkJoin` (para traer docente y turnos juntos)
import { Component, OnInit, computed, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { DocentesService } from '../../core/services/docentes.service';
import { Docente } from '../../core/models/docente.model'; // NUEVO
import { TurnoDisponible } from '../../core/models/turno.model';

interface GrupoPorFecha {
  fecha: string;
  turnos: TurnoDisponible[];
}

@Component({
  selector: 'app-seleccionar-turno',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './seleccionar-turno.component.html'
})
export class SeleccionarTurnoComponent implements OnInit {
  docenteId!: number;
  docente = signal<Docente | null>(null); // NUEVO: para mostrar el nombre y la sala
  grupos = signal<GrupoPorFecha[]>([]);
  fechaElegida = signal<string | null>(null); // NUEVO: el día que tocó la familia
  cargando = signal(true);
  error = signal<string | null>(null);

  // NUEVO: horarios del día elegido; se recalcula solo si cambia el día o los grupos
  turnosDelDia = computed(
    () => this.grupos().find((g) => g.fecha === this.fechaElegida())?.turnos ?? []
  );

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private docentesService: DocentesService
  ) {}

  ngOnInit(): void {
    this.docenteId = Number(this.route.snapshot.paramMap.get('docenteId'));

    // CAMBIADO: antes era solo turnosDisponibles(). No existe GET /docentes/{id},
    // así que el nombre se saca de la lista pública.
    forkJoin({
      docentes: this.docentesService.listar(),
      turnos: this.docentesService.turnosDisponibles(this.docenteId)
    }).subscribe({
      next: ({ docentes, turnos }) => {
        this.docente.set(docentes.find((d) => d.id === this.docenteId) ?? null); // NUEVO
        const grupos = this.agruparPorFecha(turnos);
        this.grupos.set(grupos);
        // NUEVO: se preselecciona el primer día con horarios
        this.fechaElegida.set(grupos[0]?.fecha ?? null);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar los horarios disponibles. Probá recargar la página.');
        this.cargando.set(false);
      }
    });
  }

  // NUEVO: se llama al tocar un chip de día
  elegirFecha(fecha: string): void {
    this.fechaElegida.set(fecha);
  }

  elegirTurno(turno: TurnoDisponible): void {
    this.router.navigate(['/reservar', this.docenteId, 'turno', turno.id]);
  }

  // NUEVO: "lun"
  diaCorto(fecha: string): string {
    return this.aFechaLocal(fecha).toLocaleDateString('es-AR', { weekday: 'short' }).replace('.', '');
  }

  // NUEVO: "15 oct"
  diaYMes(fecha: string): string {
    return this.aFechaLocal(fecha)
      .toLocaleDateString('es-AR', { day: 'numeric', month: 'short' })
      .replace('.', '');
  }

  // CAMBIADO: ahora usa aFechaLocal(), que hace lo mismo que antes pero reutilizable
  formatearFecha(fecha: string): string {
    return this.aFechaLocal(fecha).toLocaleDateString('es-AR', {
      weekday: 'long',
      day: 'numeric',
      month: 'long'
    });
  }

  formatearHora(hora: string): string {
    return hora.slice(0, 5);
  }

  // NUEVO: lo que antes hacías dentro de formatearFecha. "yyyy-MM-dd" se parsea a mano
  // porque new Date("2026-10-15") se interpreta en UTC y en Argentina mostraría el día anterior.
  private aFechaLocal(fecha: string): Date {
    const [anio, mes, dia] = fecha.split('-').map(Number);
    return new Date(anio, mes - 1, dia);
  }

  private agruparPorFecha(turnos: TurnoDisponible[]): GrupoPorFecha[] {
    const mapa = new Map<string, TurnoDisponible[]>();
    for (const turno of turnos) {
      const lista = mapa.get(turno.fecha) ?? [];
      lista.push(turno);
      mapa.set(turno.fecha, lista);
    }
    return Array.from(mapa.entries()).map(([fecha, turnos]) => ({ fecha, turnos }));
  }
}







/* LO QUE HABIA ANTES X LAS DUDAS
import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DocentesService } from '../../core/services/docentes.service';
import { TurnoDisponible } from '../../core/models/turno.model';

interface GrupoPorFecha {
  fecha: string;
  turnos: TurnoDisponible[];
}

@Component({
  selector: 'app-seleccionar-turno',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './seleccionar-turno.component.html'
})
export class SeleccionarTurnoComponent implements OnInit {
  docenteId!: number;
  grupos = signal<GrupoPorFecha[]>([]);
  cargando = signal(true);
  error = signal<string | null>(null);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private docentesService: DocentesService
  ) {}

  ngOnInit(): void {
    this.docenteId = Number(this.route.snapshot.paramMap.get('docenteId'));

    this.docentesService.turnosDisponibles(this.docenteId).subscribe({
      next: (turnos) => {
        this.grupos.set(this.agruparPorFecha(turnos));
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar los horarios disponibles. Probá recargar la página.');
        this.cargando.set(false);
      }
    });
  }

  elegirTurno(turno: TurnoDisponible): void {
    this.router.navigate(['/reservar', this.docenteId, 'turno', turno.id]);
  }

  formatearFecha(fecha: string): string {
    const [anio, mes, dia] = fecha.split('-').map(Number);
    const fechaLocal = new Date(anio, mes - 1, dia);
    return fechaLocal.toLocaleDateString('es-AR', { weekday: 'long', day: 'numeric', month: 'long' });
  }

  formatearHora(hora: string): string {
    return hora.slice(0, 5);
  }

  private agruparPorFecha(turnos: TurnoDisponible[]): GrupoPorFecha[] {
    const mapa = new Map<string, TurnoDisponible[]>();
    for (const turno of turnos) {
      const lista = mapa.get(turno.fecha) ?? [];
      lista.push(turno);
      mapa.set(turno.fecha, lista);
    }
    return Array.from(mapa.entries()).map(([fecha, turnos]) => ({ fecha, turnos }));
  }
}*/
