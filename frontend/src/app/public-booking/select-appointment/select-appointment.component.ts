import { Component, OnInit, computed, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { BOOKING_DAYS_AHEAD, BookingService } from '../../core/services/booking.service';
import { AvailableAppointment } from '../../core/models/appointment.model';
import { Teacher } from '../../core/models/teacher.model';
import { PublicShellComponent } from '../../shared/public-shell.component';
import { avatarColor, initials } from '../../shared/avatar.util';

/** A calendar cell. `date` is null in the blanks before day 1 and after the last day. */
interface CalendarDay {
  date: string | null; // "yyyy-MM-dd"
  number: number | null;
  count: number; // free appointments that day
}

interface Month {
  year: number;
  month: number; // 0 = January
}

@Component({
  selector: 'app-select-appointment',
  standalone: true,
  imports: [RouterLink, PublicShellComponent],
  templateUrl: './select-appointment.component.html'
})
export class SelectAppointmentComponent implements OnInit {
  teacherId!: number;
  /** Teacher chosen in the previous step, shown above the times. */
  teacher = signal<Teacher | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  /** Free appointments grouped by date ("yyyy-MM-dd"), already sorted by time. */
  appointmentsByDate = signal<Map<string, AvailableAppointment[]>>(new Map());
  /** Month currently shown in the calendar. */
  visibleMonth = signal<Month>({ year: new Date().getFullYear(), month: new Date().getMonth() });
  /** Day chosen in the calendar. */
  selectedDate = signal<string | null>(null);

  readonly weekdays = ['L', 'M', 'M', 'J', 'V', 'S', 'D'];
  readonly monthsAhead = Math.round(BOOKING_DAYS_AHEAD / 30);
  readonly initials = initials;
  readonly avatarColor = avatarColor;

  hasAppointments = computed(() => this.appointmentsByDate().size > 0);

  /** First and last month with free appointments: the calendar doesn't navigate beyond them. */
  private monthRange = computed(() => {
    const dates = Array.from(this.appointmentsByDate().keys()).sort();
    if (dates.length === 0) return null;
    return { first: this.monthOf(dates[0]), last: this.monthOf(dates[dates.length - 1]) };
  });

  canGoBack = computed(() => {
    const range = this.monthRange();
    return range !== null && this.compare(this.visibleMonth(), range.first) > 0;
  });

  canGoForward = computed(() => {
    const range = this.monthRange();
    return range !== null && this.compare(this.visibleMonth(), range.last) < 0;
  });

  monthTitle = computed(() => {
    const { year, month } = this.visibleMonth();
    const text = new Date(year, month, 1).toLocaleDateString('es-AR', { month: 'long', year: 'numeric' });
    return text.charAt(0).toUpperCase() + text.slice(1);
  });

  /** Weeks of the visible month, Monday to Sunday. */
  weeks = computed<CalendarDay[][]>(() => {
    const { year, month } = this.visibleMonth();
    const appointments = this.appointmentsByDate();
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    // getDay(): 0 = Sunday. Shifted so the week starts on Monday.
    const leadingBlanks = (new Date(year, month, 1).getDay() + 6) % 7;

    const cells: CalendarDay[] = [];
    for (let i = 0; i < leadingBlanks; i++) cells.push({ date: null, number: null, count: 0 });
    for (let day = 1; day <= daysInMonth; day++) {
      const date = `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
      cells.push({ date, number: day, count: appointments.get(date)?.length ?? 0 });
    }
    while (cells.length % 7 !== 0) cells.push({ date: null, number: null, count: 0 });

    const weeks: CalendarDay[][] = [];
    for (let i = 0; i < cells.length; i += 7) weeks.push(cells.slice(i, i + 7));
    return weeks;
  });

  /** Times of the chosen day, split into morning and afternoon. */
  dayAppointments = computed(() => {
    const date = this.selectedDate();
    const appointments = date ? this.appointmentsByDate().get(date) ?? [] : [];
    return {
      morning: appointments.filter((a) => a.startTime < '12:00'),
      afternoon: appointments.filter((a) => a.startTime >= '12:00')
    };
  });

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private bookingService: BookingService
  ) {}

  ngOnInit(): void {
    this.teacherId = Number(this.route.snapshot.paramMap.get('teacherId'));

    // Only to show the name: if it fails, the screen keeps working the same.
    this.bookingService.listTeachers().subscribe({
      next: (teachers) => this.teacher.set(teachers.find((t) => t.id === this.teacherId) ?? null)
    });

    this.bookingService.availableAppointments(this.teacherId).subscribe({
      next: (appointments) => {
        const byDate = this.groupByDate(appointments);
        this.appointmentsByDate.set(byDate);

        // Opens directly on the first day with free slots, even if it is months ahead.
        const firstDate = Array.from(byDate.keys()).sort()[0];
        if (firstDate) {
          this.visibleMonth.set(this.monthOf(firstDate));
          this.selectedDate.set(firstDate);
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar los horarios disponibles. Probá recargar la página.');
        this.loading.set(false);
      }
    });
  }

  changeMonth(delta: number): void {
    if ((delta < 0 && !this.canGoBack()) || (delta > 0 && !this.canGoForward())) return;

    const { year, month } = this.visibleMonth();
    const next = new Date(year, month + delta, 1);
    const newMonth = { year: next.getFullYear(), month: next.getMonth() };
    this.visibleMonth.set(newMonth);

    // When changing month, its first day with free slots is selected, so times from another month don't stay on screen.
    const firstOfMonth = Array.from(this.appointmentsByDate().keys())
      .sort()
      .find((date) => this.compare(this.monthOf(date), newMonth) === 0);
    this.selectedDate.set(firstOfMonth ?? null);
  }

  chooseDate(day: CalendarDay): void {
    if (day.date && day.count > 0) this.selectedDate.set(day.date);
  }

  chooseAppointment(appointment: AvailableAppointment): void {
    this.router.navigate(['/reservar', this.teacherId, 'turno', appointment.id]);
  }

  /** Text for screen readers: "lunes, 5 de octubre: 4 horarios disponibles". */
  dayLabel(day: CalendarDay): string {
    if (!day.date) return '';
    const date = this.formatDate(day.date);
    if (day.count === 0) return `${date}: sin turnos`;
    return `${date}: ${day.count} ${day.count === 1 ? 'horario disponible' : 'horarios disponibles'}`;
  }

  formatDate(date: string): string {
    const [year, month, day] = date.split('-').map(Number);
    const localDate = new Date(year, month - 1, day);
    return localDate.toLocaleDateString('es-AR', { weekday: 'long', day: 'numeric', month: 'long' });
  }

  formatTime(time: string): string {
    return time.slice(0, 5);
  }

  private monthOf(date: string): Month {
    const [year, month] = date.split('-').map(Number);
    return { year, month: month - 1 };
  }

  private compare(a: Month, b: Month): number {
    return a.year * 12 + a.month - (b.year * 12 + b.month);
  }

  private groupByDate(appointments: AvailableAppointment[]): Map<string, AvailableAppointment[]> {
    const map = new Map<string, AvailableAppointment[]>();
    for (const appointment of appointments) {
      const list = map.get(appointment.date) ?? [];
      list.push(appointment);
      map.set(appointment.date, list);
    }
    for (const list of map.values()) list.sort((a, b) => a.startTime.localeCompare(b.startTime));
    return map;
  }
}
