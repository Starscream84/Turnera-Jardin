import { Component, OnInit, computed, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { concatMap, forkJoin, from, reduce, startWith } from 'rxjs';
import { AdminTeachersService } from '../../core/services/admin-teachers.service';
import { TeacherAdmin } from '../../core/models/teacher.model';

/** Dropdown value to generate the same appointments for every active teacher. */
const ALL = 'all';

/** Same limit as the backend (AdminTeachersController.MaxGenerationMonths). */
const MAX_GENERATION_MONTHS = 6;

/** "HH:mm" → minutes since midnight. NaN if empty. */
function toMinutes(time: string): number {
  const [h, m] = time.split(':').map(Number);
  return h * 60 + m;
}

interface Weekday {
  number: number; // 0 = Sunday .. 6 = Saturday, same as System.DayOfWeek in the backend
  label: string;
}

@Component({
  selector: 'app-generate-appointments',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './generate-appointments.component.html'
})
export class GenerateAppointmentsComponent implements OnInit {
  private fb = inject(FormBuilder);
  private teachersService = inject(AdminTeachersService);

  teachers = signal<TeacherAdmin[]>([]);
  /** Addresses where an in-person interview can take place. */
  locations = signal<string[]>([]);
  loading = signal(true);
  saving = signal(false);
  error = signal<string | null>(null);
  result = signal<number | null>(null);
  /** How many teachers got appointments in the last request (for the success message). */
  teachersProcessed = signal(0);
  readonly ALL = ALL;

  readonly days: Weekday[] = [
    { number: 1, label: 'Lunes' },
    { number: 2, label: 'Martes' },
    { number: 3, label: 'Miércoles' },
    { number: 4, label: 'Jueves' },
    { number: 5, label: 'Viernes' },
    { number: 6, label: 'Sábado' },
    { number: 0, label: 'Domingo' }
  ];

  form = this.fb.nonNullable.group({
    teacherId: ['', Validators.required],
    location: ['', Validators.required],
    fromDate: ['', Validators.required],
    toDate: ['', Validators.required],
    selectedDays: this.fb.nonNullable.group({
      1: [true], 2: [true], 3: [true], 4: [true], 5: [true], 6: [false], 0: [false]
    }),
    // One or more time slots per day (e.g. 08:00–12:00 and 13:00–18:00, with the lunch break in between).
    timeSlots: this.fb.nonNullable.array([this.newTimeSlot('08:00', '12:00')]),
    durationMinutes: [20, [Validators.required, Validators.min(5), Validators.max(240)]]
  });

  private formValue = toSignal(this.form.valueChanges.pipe(startWith(this.form.getRawValue())), {
    initialValue: this.form.getRawValue()
  });

  /**
   * Preview of what will be generated: slots per day for each time range, and the minutes left over
   * at the end of a range when the duration does not divide it (e.g. 08:00–12:00 every 25 min → 9 slots,
   * 15 minutes unused).
   */
  preview = computed(() => {
    const values = this.formValue();
    const duration = Number(values.durationMinutes);
    const ranges = (values.timeSlots ?? []).map((s) => {
      const start = toMinutes(s?.startTime ?? '');
      const end = toMinutes(s?.endTime ?? '');
      if (!duration || duration <= 0 || isNaN(start) || isNaN(end) || end <= start) {
        return { slots: 0, leftover: 0, valid: false };
      }
      const slots = Math.floor((end - start) / duration);
      return { slots, leftover: (end - start) - slots * duration, valid: true };
    });
    return { ranges, perDay: ranges.reduce((total, r) => total + r.slots, 0) };
  });

  private newTimeSlot(startTime = '', endTime = '') {
    return this.fb.nonNullable.group({
      startTime: [startTime, Validators.required],
      endTime: [endTime, Validators.required]
    });
  }

  addTimeSlot(): void {
    this.form.controls.timeSlots.push(this.newTimeSlot());
  }

  removeTimeSlot(index: number): void {
    this.form.controls.timeSlots.removeAt(index);
  }

  ngOnInit(): void {
    forkJoin({
      teachers: this.teachersService.list(),
      locations: this.teachersService.interviewLocations()
    }).subscribe({
      next: ({ teachers, locations }) => {
        this.teachers.set(teachers.filter((t) => t.isActive));
        this.locations.set(locations);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar la lista de docentes y direcciones.');
        this.loading.set(false);
      }
    });
  }

  generate(): void {
    this.result.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set('Faltan datos: elegí el docente (o "Todos") y la dirección, y completá las fechas, los horarios de cada franja y la duración.');
      return;
    }

    const values = this.form.getRawValue();
    const daysOfWeek = Object.entries(values.selectedDays)
      .filter(([, checked]) => checked)
      .map(([number]) => Number(number));

    if (daysOfWeek.length === 0) {
      this.error.set('Elegí al menos un día de la semana.');
      return;
    }
    if (values.toDate < values.fromDate) {
      this.error.set('La fecha "hasta" no puede ser anterior a la fecha "desde".');
      return;
    }
    const [fy, fm, fd] = values.fromDate.split('-').map(Number);
    const limit = new Date(fy, fm - 1 + MAX_GENERATION_MONTHS, fd);
    const [ty, tm, td] = values.toDate.split('-').map(Number);
    if (new Date(ty, tm - 1, td) > limit) {
      this.error.set(`Se pueden generar turnos para hasta ${MAX_GENERATION_MONTHS} meses por vez. Achicá el período.`);
      return;
    }
    if (this.preview().ranges.some((r) => r.valid && r.slots === 0)) {
      this.error.set('En alguna franja no entra ni un turno completo con esa duración. Alargá la franja o acortá la duración.');
      return;
    }
    const timeSlots = [...values.timeSlots].sort((a, b) => a.startTime.localeCompare(b.startTime));
    if (timeSlots.some((s) => s.endTime <= s.startTime)) {
      this.error.set('En cada franja, la hora de fin tiene que ser posterior a la hora de inicio.');
      return;
    }
    if (timeSlots.some((s, i) => i > 0 && s.startTime < timeSlots[i - 1].endTime)) {
      this.error.set('Las franjas horarias se superponen. Revisá los horarios.');
      return;
    }

    // One request per teacher + time slot combination.
    const requests = timeSlots.map((s) => ({
      fromDate: values.fromDate,
      toDate: values.toDate,
      daysOfWeek,
      startTime: this.timeWithSeconds(s.startTime),
      endTime: this.timeWithSeconds(s.endTime),
      durationMinutes: values.durationMinutes,
      location: values.location
    }));

    const ids =
      values.teacherId === ALL ? this.teachers().map((t) => t.id) : [Number(values.teacherId)];

    this.saving.set(true);
    this.error.set(null);

    // Requests are sent one at a time, in order.
    const tasks = ids.flatMap((id) => requests.map((data) => ({ id, data })));

    from(tasks)
      .pipe(
        concatMap(({ id, data }) => this.teachersService.generateAppointments(id, data)),
        reduce((total, response) => total + response.created, 0)
      )
      .subscribe({
        next: (created) => {
          this.saving.set(false);
          this.teachersProcessed.set(ids.length);
          this.result.set(created);
        },
        error: (response: HttpErrorResponse) => {
          this.saving.set(false);
          this.error.set(this.errorMessage(response));
        }
      });
  }

  /**
   * <input type="time"> returns "HH:mm", but the backend (.NET TimeOnly) only accepts
   * "HH:mm:ss": without the seconds it answers 400 and nothing is generated.
   */
  private timeWithSeconds(time: string): string {
    return time.length === 5 ? `${time}:00` : time;
  }

  private errorMessage(response: HttpErrorResponse): string {
    if (response.status === 0) {
      return 'No pudimos conectarnos con el servidor. Si recién lo abrís, puede tardar un minuto en despertar: probá de nuevo.';
    }
    if (response.status === 401 || response.status === 403) {
      return 'Tu sesión venció o tu usuario no tiene permiso para generar turnos. Volvé a iniciar sesión.';
    }
    // The backend's own validation errors come as { message: "..." }.
    const message = response.error?.message;
    return typeof message === 'string' ? message : 'No pudimos generar los turnos. Revisá los datos ingresados.';
  }
}
