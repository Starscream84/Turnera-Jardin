import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdminDocentesService } from '../../core/services/admin-docentes.service';
import { DocenteAdmin } from '../../core/models/docente.model';

@Component({
  selector: 'app-docentes-list',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './docentes-list.component.html'
})
export class DocentesListComponent implements OnInit {
  docentes = signal<DocenteAdmin[]>([]);
  cargando = signal(true);
  error = signal<string | null>(null);

  constructor(private service: AdminDocentesService) {}

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.service.listar().subscribe({
      next: (docentes) => {
        this.docentes.set(docentes);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar los docentes.');
        this.cargando.set(false);
      }
    });
  }

  alternarActivo(docente: DocenteAdmin): void {
    if (!docente.activo) return; // reactivar no está soportado desde acá, se hace editando
    const confirmado = confirm(`¿Desactivar a ${docente.nombre} ${docente.apellido}? Ya no va a aparecer en la agenda pública.`);
    if (!confirmado) return;

    this.service.desactivar(docente.id).subscribe({
      next: () => this.cargar(),
      error: () => this.error.set('No pudimos desactivar al docente.')
    });
  }
}
