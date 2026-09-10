import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AdminDocentesService } from '../../core/services/admin-docentes.service';

@Component({
  selector: 'app-docente-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './docente-form.component.html'
})
export class DocenteFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private service = inject(AdminDocentesService);

  docenteId: number | null = null;
  cargando = signal(false);
  guardando = signal(false);
  error = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    nombre: ['', [Validators.required]],
    apellido: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    sala: [''],
    activo: [true]
  });

  get esEdicion(): boolean {
    return this.docenteId !== null;
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.docenteId = Number(idParam);
      this.cargando.set(true);
      this.service.listar().subscribe({
        next: (docentes) => {
          const docente = docentes.find((d) => d.id === this.docenteId);
          if (docente) {
            this.form.patchValue({ ...docente, sala: docente.sala ?? '' });
          }
          this.cargando.set(false);
        },
        error: () => {
          this.error.set('No pudimos cargar los datos del docente.');
          this.cargando.set(false);
        }
      });
    }
  }

  guardar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.guardando.set(true);
    this.error.set(null);
    const datos = this.form.getRawValue();

    const peticion: Observable<unknown> = this.esEdicion
      ? this.service.actualizar(this.docenteId!, datos)
      : this.service.crear(datos);

    peticion.subscribe({
      next: () => {
        this.guardando.set(false);
        this.router.navigate(['/admin/docentes']);
      },
      error: (err: HttpErrorResponse) => {
        this.guardando.set(false);
        this.error.set(err.status === 409 ? 'Ya existe un docente con ese email.' : 'No pudimos guardar los cambios.');
      }
    });
  }
}
