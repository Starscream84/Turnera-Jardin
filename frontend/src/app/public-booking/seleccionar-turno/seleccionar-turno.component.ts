import { Component, OnInit, computed, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DIAS_ANTICIPACION_RESERVA, DocentesService } from '../../core/services/docentes.service';
import { TurnoDisponible } from '../../core/models/turno.model';
import { Docente } from '../../core/models/docente.model';
import { PublicShellComponent } from '../../shared/public-shell.component';
import { colorAvatar, iniciales } from '../../shared/avatar.util';

/** Una celda del calendario. `fecha` es null en los huecos de antes del día 1 y después del último. */
interface DiaCalendario {
  fecha: string | null; // "yyyy-MM-dd"
  numero: number | null;
  cantidad: number; // turnos libres ese día
}

interface Mes {
  anio: number;
  mes: number; // 0 = enero
}

@Component({
  selector: 'app-seleccionar-turno',
  standalone: true,
  imports: [RouterLink, PublicShellComponent],
  templateUrl: './seleccionar-turno.component.html'
})
export class SeleccionarTurnoComponent implements OnInit {
  docenteId!: number;
  /** Docente elegido en el paso anterior, para mostrarlo arriba de los horarios. */
  docente = signal<Docente | null>(null);
  cargando = signal(true);
  error = signal<string | null>(null);

  /** Turnos libres agrupados por fecha ("yyyy-MM-dd"), ya ordenados por hora. */
  turnosPorFecha = signal<Map<string, TurnoDisponible[]>>(new Map());
  /** Mes que se está mirando en el calendario. */
  mesVisible = signal<Mes>({ anio: new Date().getFullYear(), mes: new Date().getMonth() });
  /** Día elegido en el calendario. */
  fechaSeleccionada = signal<string | null>(null);

  readonly diasSemana = ['L', 'M', 'M', 'J', 'V', 'S', 'D'];
  readonly mesesAnticipacion = Math.round(DIAS_ANTICIPACION_RESERVA / 30);
  readonly iniciales = iniciales;
  readonly colorAvatar = colorAvatar;

  hayTurnos = computed(() => this.turnosPorFecha().size > 0);

  /** Primer y último mes con turnos libres: el calendario no navega más allá. */
  private rangoMeses = computed(() => {
    const fechas = Array.from(this.turnosPorFecha().keys()).sort();
    if (fechas.length === 0) return null;
    return { primero: this.mesDe(fechas[0]), ultimo: this.mesDe(fechas[fechas.length - 1]) };
  });

  puedeIrAtras = computed(() => {
    const rango = this.rangoMeses();
    return rango !== null && this.comparar(this.mesVisible(), rango.primero) > 0;
  });

  puedeIrAdelante = computed(() => {
    const rango = this.rangoMeses();
    return rango !== null && this.comparar(this.mesVisible(), rango.ultimo) < 0;
  });

  tituloMes = computed(() => {
    const { anio, mes } = this.mesVisible();
    const texto = new Date(anio, mes, 1).toLocaleDateString('es-AR', { month: 'long', year: 'numeric' });
    return texto.charAt(0).toUpperCase() + texto.slice(1);
  });

  /** Semanas del mes visible, de lunes a domingo. */
  semanas = computed<DiaCalendario[][]>(() => {
    const { anio, mes } = this.mesVisible();
    const turnos = this.turnosPorFecha();
    const diasDelMes = new Date(anio, mes + 1, 0).getDate();
    // getDay(): 0 = domingo. Se corre para que la semana empiece en lunes.
    const huecosAlInicio = (new Date(anio, mes, 1).getDay() + 6) % 7;

    const celdas: DiaCalendario[] = [];
    for (let i = 0; i < huecosAlInicio; i++) celdas.push({ fecha: null, numero: null, cantidad: 0 });
    for (let dia = 1; dia <= diasDelMes; dia++) {
      const fecha = `${anio}-${String(mes + 1).padStart(2, '0')}-${String(dia).padStart(2, '0')}`;
      celdas.push({ fecha, numero: dia, cantidad: turnos.get(fecha)?.length ?? 0 });
    }
    while (celdas.length % 7 !== 0) celdas.push({ fecha: null, numero: null, cantidad: 0 });

    const semanas: DiaCalendario[][] = [];
    for (let i = 0; i < celdas.length; i += 7) semanas.push(celdas.slice(i, i + 7));
    return semanas;
  });

  /** Horarios del día elegido, separados en mañana y tarde. */
  turnosDelDia = computed(() => {
    const fecha = this.fechaSeleccionada();
    const turnos = fecha ? this.turnosPorFecha().get(fecha) ?? [] : [];
    return {
      manana: turnos.filter((t) => t.horaInicio < '12:00'),
      tarde: turnos.filter((t) => t.horaInicio >= '12:00')
    };
  });

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private docentesService: DocentesService
  ) {}

  ngOnInit(): void {
    this.docenteId = Number(this.route.snapshot.paramMap.get('docenteId'));

    // Solo para mostrar el nombre: si falla, la pantalla sigue funcionando igual.
    this.docentesService.listar().subscribe({
      next: (docentes) => this.docente.set(docentes.find((d) => d.id === this.docenteId) ?? null)
    });

    this.docentesService.turnosDisponibles(this.docenteId).subscribe({
      next: (turnos) => {
        const porFecha = this.agruparPorFecha(turnos);
        this.turnosPorFecha.set(porFecha);

        // Se abre directamente en el primer día con lugar, aunque falten meses.
        const primeraFecha = Array.from(porFecha.keys()).sort()[0];
        if (primeraFecha) {
          this.mesVisible.set(this.mesDe(primeraFecha));
          this.fechaSeleccionada.set(primeraFecha);
        }
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar los horarios disponibles. Probá recargar la página.');
        this.cargando.set(false);
      }
    });
  }

  cambiarMes(delta: number): void {
    if ((delta < 0 && !this.puedeIrAtras()) || (delta > 0 && !this.puedeIrAdelante())) return;

    const { anio, mes } = this.mesVisible();
    const nuevo = new Date(anio, mes + delta, 1);
    const mesNuevo = { anio: nuevo.getFullYear(), mes: nuevo.getMonth() };
    this.mesVisible.set(mesNuevo);

    // Al cambiar de mes se elige su primer día con lugar, para no dejar horarios de otro mes a la vista.
    const primeraDelMes = Array.from(this.turnosPorFecha().keys())
      .sort()
      .find((fecha) => this.comparar(this.mesDe(fecha), mesNuevo) === 0);
    this.fechaSeleccionada.set(primeraDelMes ?? null);
  }

  elegirFecha(dia: DiaCalendario): void {
    if (dia.fecha && dia.cantidad > 0) this.fechaSeleccionada.set(dia.fecha);
  }

  elegirTurno(turno: TurnoDisponible): void {
    this.router.navigate(['/reservar', this.docenteId, 'turno', turno.id]);
  }

  /** Texto para lectores de pantalla: "lunes, 5 de octubre: 4 horarios disponibles". */
  etiquetaDia(dia: DiaCalendario): string {
    if (!dia.fecha) return '';
    const fecha = this.formatearFecha(dia.fecha);
    if (dia.cantidad === 0) return `${fecha}: sin turnos`;
    return `${fecha}: ${dia.cantidad} ${dia.cantidad === 1 ? 'horario disponible' : 'horarios disponibles'}`;
  }

  formatearFecha(fecha: string): string {
    const [anio, mes, dia] = fecha.split('-').map(Number);
    const fechaLocal = new Date(anio, mes - 1, dia);
    return fechaLocal.toLocaleDateString('es-AR', { weekday: 'long', day: 'numeric', month: 'long' });
  }

  formatearHora(hora: string): string {
    return hora.slice(0, 5);
  }

  private mesDe(fecha: string): Mes {
    const [anio, mes] = fecha.split('-').map(Number);
    return { anio, mes: mes - 1 };
  }

  private comparar(a: Mes, b: Mes): number {
    return a.anio * 12 + a.mes - (b.anio * 12 + b.mes);
  }

  private agruparPorFecha(turnos: TurnoDisponible[]): Map<string, TurnoDisponible[]> {
    const mapa = new Map<string, TurnoDisponible[]>();
    for (const turno of turnos) {
      const lista = mapa.get(turno.fecha) ?? [];
      lista.push(turno);
      mapa.set(turno.fecha, lista);
    }
    for (const lista of mapa.values()) lista.sort((a, b) => a.horaInicio.localeCompare(b.horaInicio));
    return mapa;
  }
}
