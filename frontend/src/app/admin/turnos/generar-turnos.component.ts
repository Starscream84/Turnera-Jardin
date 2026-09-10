import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AdminDocentesService } from '../../core/services/admin-docentes.service';
import { DocenteAdmin } from '../../core/models/docente.model';

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
    horaInicio: ['08:00', Validators.required],
    horaFin: ['12:00', Validators.required],
    duracionMinutos: [20, [Validators.required, Validators.min(5), Validators.max(240)]]
  });

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
    if (this.form.invalid) {
      this.form.markAllAsTouched();
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

    this.guardando.set(true);
    this.error.set(null);
    this.resultado.set(null);

    this.docentesService
      .generarTurnos(Number(valores.docenteId), {
        fechaDesde: valores.fechaDesde,
        fechaHasta: valores.fechaHasta,
        diasSemana,
        horaInicio: valores.horaInicio,
        horaFin: valores.horaFin,
        duracionMinutos: valores.duracionMinutos
      })
      .subscribe({
        next: (respuesta) => {
          this.guardando.set(false);
          this.resultado.set(respuesta.creados);
        },
        error: () => {
          this.guardando.set(false);
          this.error.set('No pudimos generar los turnos. Revisá los datos ingresados.');
        }
      });
  }
}
