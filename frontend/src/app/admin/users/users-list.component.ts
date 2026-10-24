import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { AdminUsersService } from '../../core/services/admin-users.service';
import { AdminTeachersService } from '../../core/services/admin-teachers.service';
import { UserAdmin } from '../../core/models/user.model';
import { TeacherAdmin } from '../../core/models/teacher.model';
import { environment } from '../../../environments/environment';
import { avatarColor, initials } from '../../shared/avatar.util';

/**
 * Management screen for panel logins: create each teacher's individual login
 * (so they only see their own appointments) and reset passwords.
 */
@Component({
  selector: 'app-users-list',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './users-list.component.html'
})
export class UsersListComponent implements OnInit {
  private fb = inject(FormBuilder);
  private usersService = inject(AdminUsersService);
  private teachersService = inject(AdminTeachersService);

  users = signal<UserAdmin[]>([]);
  teachersWithoutUser = signal<TeacherAdmin[]>([]);
  loading = signal(true);
  creating = signal(false);
  error = signal<string | null>(null);
  generatedCredentials = signal<{ email: string; temporaryPassword: string } | null>(null);

  readonly initials = initials;
  readonly avatarColor = avatarColor;

  createForm = this.fb.nonNullable.group({
    teacherId: ['', Validators.required],
    password: ['']
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      users: this.usersService.list(),
      teachers: this.teachersService.list()
    }).subscribe({
      next: ({ users, teachers }) => {
        this.users.set(users);
        const idsWithUser = new Set(users.map((u) => u.teacherId).filter((id): id is number => id !== null));
        this.teachersWithoutUser.set(teachers.filter((t) => t.isActive && !idsWithUser.has(t.id)));
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar los usuarios.');
        this.loading.set(false);
      }
    });
  }

  createForTeacher(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const values = this.createForm.getRawValue();
    this.creating.set(true);
    this.error.set(null);
    this.generatedCredentials.set(null);

    this.usersService
      .create({ teacherId: Number(values.teacherId), password: values.password || undefined })
      .subscribe({
        next: (created) => {
          this.creating.set(false);
          this.generatedCredentials.set({ email: created.email, temporaryPassword: created.temporaryPassword });
          this.createForm.reset({ teacherId: '', password: '' });
          this.load();
        },
        error: (err) => {
          this.creating.set(false);
          this.error.set(err?.error?.message ?? 'No pudimos crear el usuario.');
        }
      });
  }

  resetPassword(user: UserAdmin): void {
    const confirmed = confirm(`¿Restablecer la contraseña de ${user.fullName}? Se va a generar una contraseña temporal nueva.`);
    if (!confirmed) return;

    this.error.set(null);
    this.generatedCredentials.set(null);

    this.usersService.resetPassword(user.id).subscribe({
      next: (result) => {
        this.generatedCredentials.set({ email: result.email, temporaryPassword: result.temporaryPassword });
      },
      error: () => this.error.set('No pudimos restablecer la contraseña.')
    });
  }

  toggleActive(user: UserAdmin): void {
    const verb = user.isActive ? 'desactivar' : 'activar';
    const confirmed = confirm(`¿Seguro que querés ${verb} el acceso de ${user.fullName}?`);
    if (!confirmed) return;

    this.error.set(null);
    const action = user.isActive ? this.usersService.deactivate(user.id) : this.usersService.activate(user.id);

    action.subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(err?.error?.message ?? `No pudimos ${verb} el acceso.`)
    });
  }

  closeCredentials(): void {
    this.generatedCredentials.set(null);
  }

  fullPhotoUrl(user: UserAdmin): string | null {
    return user.photoUrl ? `${environment.filesUrl}${user.photoUrl}` : null;
  }

  roleLabel(user: UserAdmin): string {
    switch (user.role) {
      case 'Admin': return 'Administrador';
      case 'Coordinator': return 'Coordinador';
      case 'Teacher': return 'Docente';
    }
  }
}
