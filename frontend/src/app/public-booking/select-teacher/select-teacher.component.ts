import { Component, OnInit, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { BookingService } from '../../core/services/booking.service';
import { Teacher } from '../../core/models/teacher.model';
import { PublicShellComponent } from '../../shared/public-shell.component';
import { avatarColor, initials } from '../../shared/avatar.util';

@Component({
  selector: 'app-select-teacher',
  standalone: true,
  imports: [PublicShellComponent],
  templateUrl: './select-teacher.component.html'
})
export class SelectTeacherComponent implements OnInit {
  teachers = signal<Teacher[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  /** Search box text: filters by name or classroom, ignoring case and accents. */
  searchText = signal('');
  filteredTeachers = computed(() => {
    const text = this.normalize(this.searchText());
    if (!text) return this.teachers();
    return this.teachers().filter((t) => this.normalize(`${t.fullName} ${t.classroom ?? ''}`).includes(text));
  });

  readonly initials = initials;
  readonly avatarColor = avatarColor;

  constructor(private bookingService: BookingService, private router: Router) {}

  ngOnInit(): void {
    this.bookingService.listTeachers().subscribe({
      next: (teachers) => {
        this.teachers.set(teachers);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar la lista de docentes. Probá recargar la página.');
        this.loading.set(false);
      }
    });
  }

  search(event: Event): void {
    this.searchText.set((event.target as HTMLInputElement).value);
  }

  choose(teacher: Teacher): void {
    this.router.navigate(['/reservar', teacher.id]);
  }

  private normalize(text: string): string {
    return text.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().trim();
  }
}
