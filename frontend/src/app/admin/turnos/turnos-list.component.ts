import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { AdminTurnosService } from '../../core/services/admin-turnos.service';
import { AdminDocentesService } from '../../core/services/admin-docentes.service';
import { AuthService } from '../../core/services/auth.service';
import { TurnoAdmin, EstadoTurno } from '../../core/models/turno.model';
import { DocenteAdmin } from '../../core/models/docente.model';

@Component({
  selector: 'app-turnos-list',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './turnos-list.component.html'
})
export class TurnosListComponent implements OnInit {
  private fb = inject(FormBuilder);
  private service = inject(AdminTurnosService);
  private docentesService = inject(AdminDocentesService);
  auth = inject(AuthService);

  turnos = signal<TurnoAdmin[]>([]);
  docentes = signal<DocenteAdmin[]>([]);
  cargando = signal(true);
  error = signal<string | null>(null);

  filtro = this.fb.nonNullable.group({
    docenteId: [''],
    desde: [''],
    hasta: [''],
    estado: ['' as EstadoTurno | '']
  });

  ngOnInit(): void {
    if (this.auth.rol() === 'Admin' || this.auth.rol() === 'Coordinador') {
      this.docentesService.listar().subscribe({ next: (docentes) => this.docentes.set(docentes) });
    }
    this.buscar();
  }

  buscar(): void {
    this.cargando.set(true);
    this.error.set(null);
    const valores = this.filtro.getRawValue();

    this.service
      .listar({
        docenteId: valores.docenteId ? Number(valores.docenteId) : undefined,
        desde: valores.desde || undefined,
        hasta: valores.hasta || undefined,
        estado: valores.estado || undefined
      })
      .subscribe({
        next: (turnos) => {
          this.turnos.set(turnos);
          this.cargando.set(false);
        },
        error: () => {
          this.error.set('No pudimos cargar los turnos.');
          this.cargando.set(false);
        }
      });
  }

  cancelar(turno: TurnoAdmin): void {
    const confirmado = confirm(`¿Cancelar el turno de ${turno.nombreNino} (${turno.nombrePadre})?`);
    if (!confirmado) return;

    this.service.cancelar(turno.id).subscribe({
      next: () => this.buscar(),
      error: () => this.error.set('No pudimos cancelar el turno.')
    });
  }

  eliminar(turno: TurnoAdmin): void {
    const confirmado = confirm('¿Eliminar esta franja horaria disponible?');
    if (!confirmado) return;

    this.service.eliminar(turno.id).subscribe({
      next: () => this.buscar(),
      error: () => this.error.set('No pudimos eliminar el turno.')
    });
  }

  formatearFecha(fecha: string): string {
    const [anio, mes, dia] = fecha.split('-').map(Number);
    return new Date(anio, mes - 1, dia).toLocaleDateString('es-AR');
  }

  formatearHora(hora: string): string {
    return hora.slice(0, 5);
  }

  claseBadge(estado: EstadoTurno): string {
    return {
      Disponible: 'badge-disponible',
      Reservado: 'badge-reservado',
      Cancelado: 'badge-cancelado',
      Completado: 'badge-completado'
    }[estado];
  }
}
