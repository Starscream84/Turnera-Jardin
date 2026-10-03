import { Component, OnInit, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { DocentesService } from '../../core/services/docentes.service';
import { Docente } from '../../core/models/docente.model';
import { PublicShellComponent } from '../../shared/public-shell.component';
import { colorAvatar, iniciales } from '../../shared/avatar.util';

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

  /** Texto del buscador: filtra por nombre o sala, sin distinguir mayúsculas ni acentos. */
  busqueda = signal('');
  docentesFiltrados = computed(() => {
    const texto = this.normalizar(this.busqueda());
    if (!texto) return this.docentes();
    return this.docentes().filter((d) => this.normalizar(`${d.nombreCompleto} ${d.sala ?? ''}`).includes(texto));
  });

  readonly iniciales = iniciales;
  readonly colorAvatar = colorAvatar;

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

  buscar(evento: Event): void {
    this.busqueda.set((evento.target as HTMLInputElement).value);
  }

  elegir(docente: Docente): void {
    this.router.navigate(['/reservar', docente.id]);
  }

  private normalizar(texto: string): string {
    return texto.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().trim();
  }
}
