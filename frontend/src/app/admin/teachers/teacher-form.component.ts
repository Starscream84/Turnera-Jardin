import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AdminTeachersService } from '../../core/services/admin-teachers.service';
import { TeacherCycle } from '../../core/models/teacher.model';

@Component({
  selector: 'app-teacher-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './teacher-form.component.html'
})
export class TeacherFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private service = inject(AdminTeachersService);

  teacherId: number | null = null;
  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required]],
    lastName: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    classroom: [''],
    cycle: ['' as TeacherCycle | ''],
    isActive: [true]
  });

  get isEdit(): boolean {
    return this.teacherId !== null;
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.teacherId = Number(idParam);
      this.loading.set(true);
      this.service.list().subscribe({
        next: (teachers) => {
          const teacher = teachers.find((t) => t.id === this.teacherId);
          if (teacher) {
            this.form.patchValue({ ...teacher, classroom: teacher.classroom ?? '', cycle: teacher.cycle ?? '' });
          }
          this.loading.set(false);
        },
        error: () => {
          this.error.set('No pudimos cargar los datos del docente.');
          this.loading.set(false);
        }
      });
    }
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const { cycle, ...rest } = this.form.getRawValue();
    const data = { ...rest, cycle: cycle || null };

    const request: Observable<unknown> = this.isEdit
      ? this.service.update(this.teacherId!, data)
      : this.service.create(data);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.router.navigate(['/admin/docentes']);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.error.set(err.status === 409 ? 'Ya existe un docente con ese email.' : 'No pudimos guardar los cambios.');
      }
    });
  }
}
