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
}
