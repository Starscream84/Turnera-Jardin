import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdminTeachersService } from '../../core/services/admin-teachers.service';
import { TeacherAdmin } from '../../core/models/teacher.model';

@Component({
  selector: 'app-teachers-list',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './teachers-list.component.html'
})
export class TeachersListComponent implements OnInit {
  teachers = signal<TeacherAdmin[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  constructor(private service: AdminTeachersService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service.list().subscribe({
      next: (teachers) => {
        this.teachers.set(teachers);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar los docentes.');
        this.loading.set(false);
      }
    });
  }

  deactivate(teacher: TeacherAdmin): void {
    if (!teacher.isActive) return; // reactivating is not supported from here, it's done by editing
    const confirmed = confirm(`¿Desactivar a ${teacher.firstName} ${teacher.lastName}? Ya no va a aparecer en la agenda pública.`);
    if (!confirmed) return;

    this.service.deactivate(teacher.id).subscribe({
      next: () => this.load(),
      error: () => this.error.set('No pudimos desactivar al docente.')
    });
  }
}
