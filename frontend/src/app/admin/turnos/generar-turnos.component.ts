import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { concatMap, from, reduce } from 'rxjs';
import { AdminDocentesService } from '../../core/services/admin-docentes.service';
import { DocenteAdmin } from '../../core/models/docente.model';

/** Valor del desplegable para generar los mismos turnos a todos los docentes activos. */
const TODOS = 'todos';

interface DiaSemana {
  numero: number; // 0=Domingo .. 6=Sábado, igual que System.DayOfWeek en el backend
  etiqueta: string;
}

@Component({
  selector: 'app-generar-turnos',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './generar-turnos.component.html'
})
export class GenerarTurnosComponent implements OnInit {
  private fb = inject(FormBuilder);
  private docentesService = inject(AdminDocentesService);

  docentes = signal<DocenteAdmin[]>([]);
  cargando = signal(true);
  guardando = signal(false);
  error = signal<string | null>(null);
  resultado = signal<number | null>(null);
  /** A cuántos docentes se les generaron turnos en el último pedido (para el mensaje de éxito). */
  docentesProcesados = signal(0);
  readonly TODOS = TODOS;

  readonly dias: DiaSemana[] = [
    { numero: 1, etiqueta: 'Lunes' },
    { numero: 2, etiqueta: 'Martes' },
    { numero: 3, etiqueta: 'Miércoles' },
    { numero: 4, etiqueta: 'Jueves' },
    { numero: 5, etiqueta: 'Viernes' },
    { numero: 6, etiqueta: 'Sábado' },
    { numero: 0, etiqueta: 'Domingo' }
  ];

  form = this.fb.nonNullable.group({
    docenteId: ['', Validators.required],
    fechaDesde: ['', Validators.required],
    fechaHasta: ['', Validators.required],
    diasSeleccionados: this.fb.nonNullable.group({
      1: [true], 2: [true], 3: [true], 4: [true], 5: [true], 6: [false], 0: [false]
    }),
    // Una o más franjas por día (ej. 08:00–12:00 y 13:00–18:00, con el corte del mediodía en el medio).
    franjas: this.fb.nonNullable.array([this.nuevaFranja('08:00', '12:00')]),
    duracionMinutos: [20, [Validators.required, Validators.min(5), Validators.max(240)]]
  });

  private nuevaFranja(horaInicio = '', horaFin = '') {
    return this.fb.nonNullable.group({
      horaInicio: [horaInicio, Validators.required],
      horaFin: [horaFin, Validators.required]
    });
  }

  agregarFranja(): void {
    this.form.controls.franjas.push(this.nuevaFranja());
  }

  quitarFranja(indice: number): void {
    this.form.controls.franjas.removeAt(indice);
  }

  ngOnInit(): void {
    this.docentesService.listar().subscribe({
      next: (docentes) => {
        this.docentes.set(docentes.filter((d) => d.activo));
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar la lista de docentes.');
        this.cargando.set(false);
      }
    });
  }

  generar(): void {
    this.resultado.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set('Faltan datos: elegí el docente (o "Todos") y completá las fechas, los horarios de cada franja y la duración.');
      return;
    }

    const valores = this.form.getRawValue();
    const diasSemana = Object.entries(valores.diasSeleccionados)
      .filter(([, marcado]) => marcado)
      .map(([numero]) => Number(numero));

    if (diasSemana.length === 0) {
      this.error.set('Elegí al menos un día de la semana.');
      return;
    }
    if (valores.fechaHasta < valores.fechaDesde) {
      this.error.set('La fecha "hasta" no puede ser anterior a la fecha "desde".');
      return;
    }
    const franjas = [...valores.franjas].sort((a, b) => a.horaInicio.localeCompare(b.horaInicio));
    if (franjas.some((f) => f.horaFin <= f.horaInicio)) {
      this.error.set('En cada franja, la hora de fin tiene que ser posterior a la hora de inicio.');
      return;
    }
    if (franjas.some((f, i) => i > 0 && f.horaInicio < franjas[i - 1].horaFin)) {
      this.error.set('Las franjas horarias se superponen. Revisá los horarios.');
      return;
    }

    // Un pedido por cada combinación docente + franja.
    const pedidos = franjas.map((f) => ({
      fechaDesde: valores.fechaDesde,
      fechaHasta: valores.fechaHasta,
      diasSemana,
      horaInicio: this.horaConSegundos(f.horaInicio),
      horaFin: this.horaConSegundos(f.horaFin),
      duracionMinutos: valores.duracionMinutos
    }));

    const ids =
      valores.docenteId === TODOS ? this.docentes().map((d) => d.id) : [Number(valores.docenteId)];

    this.guardando.set(true);
    this.error.set(null);

    // Se manda de a un pedido por vez (SQLite no admite escrituras en paralelo).
    const tareas = ids.flatMap((id) => pedidos.map((datos) => ({ id, datos })));

    from(tareas)
      .pipe(
        concatMap(({ id, datos }) => this.docentesService.generarTurnos(id, datos)),
        reduce((total, respuesta) => total + respuesta.creados, 0)
      )
      .subscribe({
        next: (creados) => {
          this.guardando.set(false);
          this.docentesProcesados.set(ids.length);
          this.resultado.set(creados);
        },
        error: (respuesta: HttpErrorResponse) => {
          this.guardando.set(false);
          this.error.set(this.mensajeDeError(respuesta));
        }
      });
  }

  /**
   * <input type="time"> devuelve "HH:mm", pero el backend (TimeOnly de .NET) solo acepta
   * "HH:mm:ss": sin los segundos responde 400 y no se genera nada.
   */
  private horaConSegundos(hora: string): string {
    return hora.length === 5 ? `${hora}:00` : hora;
  }

  private mensajeDeError(respuesta: HttpErrorResponse): string {
    if (respuesta.status === 0) {
      return 'No pudimos conectarnos con el servidor. Si recién lo abrís, puede tardar un minuto en despertar: probá de nuevo.';
    }
    if (respuesta.status === 401 || respuesta.status === 403) {
      return 'Tu sesión venció o tu usuario no tiene permiso para generar turnos. Volvé a iniciar sesión.';
    }
    // Los errores de validación propios del backend vienen como { mensaje: "..." }.
    const mensaje = respuesta.error?.mensaje;
    return typeof mensaje === 'string' ? mensaje : 'No pudimos generar los turnos. Revisá los datos ingresados.';
  }
}
