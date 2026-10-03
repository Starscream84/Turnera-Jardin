import { Component, OnInit, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { DocentesService } from '../../core/services/docentes.service';
import { Docente } from '../../core/models/docente.model';
import { PublicShellComponent } from '../../shared/public-shell.component';

@Component({
  selector: 'app-seleccionar-docente',
  standalone: true,
  imports: [PublicShellComponent],
  templateUrl: './seleccionar-docente.component.html'
})
export class SeleccionarDocenteComponent implements OnInit {
  docentes = signal<Docente[]>([]);
  cargando = signal(true);
  error = signal<string | null>(null);

  /** Id del docente elegido en el desplegable ('' mientras no se eligió ninguno). */
  docenteId = signal('');

  /** Lista del desplegable, ordenada alfabéticamente para encontrar el nombre rápido. */
  docentesOrdenados = computed(() =>
    [...this.docentes()].sort((a, b) => a.nombreCompleto.localeCompare(b.nombreCompleto, 'es'))
  );

  constructor(private docentesService: DocentesService, private router: Router) {}

  ngOnInit(): void {
    this.docentesService.listar().subscribe({
      next: (docentes) => {
        this.docentes.set(docentes);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar la lista de docentes. Probá recargar la página.');
        this.cargando.set(false);
      }
    });
  }

  seleccionar(evento: Event): void {
    this.docenteId.set((evento.target as HTMLSelectElement).value);
  }

  continuar(): void {
    if (!this.docenteId()) return;
    this.router.navigate(['/reservar', this.docenteId()]);
  }
}
