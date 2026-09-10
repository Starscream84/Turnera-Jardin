import { Component, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DocentesService } from '../../core/services/docentes.service';
import { Docente } from '../../core/models/docente.model';

@Component({
  selector: 'app-seleccionar-docente',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './seleccionar-docente.component.html'
})
export class SeleccionarDocenteComponent implements OnInit {
  docentes = signal<Docente[]>([]);
  cargando = signal(true);
  error = signal<string | null>(null);

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

  elegir(docente: Docente): void {
    this.router.navigate(['/reservar', docente.id]);
  }
}
