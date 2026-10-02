import { Component, OnInit, computed, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DocentesService } from '../../core/services/docentes.service';
import { Docente } from '../../core/models/docente.model';

const GRUPO_OTRAS_ACTIVIDADES = 'Otras actividades';

// Paleta simple para los avatares; se asigna por posición, sin ningún significado especial.
const COLORES_AVATAR = ['#6b7fd7', '#d76b9e', '#d7a26b', '#6bb3d7', '#8e6bd7', '#6bd79e', '#d76b6b'];

interface GrupoPorSala {
  sala: string;
  docentes: Docente[];
}

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

  // Paso 1 = elegir sala, paso 2 = elegir docente dentro de la sala elegida.
  salaElegida = signal<string | null>(null);

  // Agrupa por sala; lo que no empieza con "Sala " (ej: "Música", "Educación Física")
  // cae junto en "Otras actividades" al final, en vez de generar un grupo por cada materia.
  grupos = computed<GrupoPorSala[]>(() => {
    const mapa = new Map<string, Docente[]>();
    for (const docente of this.docentes()) {
      const nombreGrupo = this.esSala(docente.sala) ? docente.sala! : GRUPO_OTRAS_ACTIVIDADES;
      const lista = mapa.get(nombreGrupo) ?? [];
      lista.push(docente);
      mapa.set(nombreGrupo, lista);
    }

    const entradas = Array.from(mapa.entries());
    entradas.sort((a, b) => {
      if (a[0] === GRUPO_OTRAS_ACTIVIDADES) return 1;
      if (b[0] === GRUPO_OTRAS_ACTIVIDADES) return -1;
      return 0;
    });

    return entradas.map(([sala, docentes]) => ({ sala, docentes }));
  });

  // Docentes de la sala elegida en el paso 1; vacío mientras no se eligió ninguna.
  docentesDeLaSala = computed<Docente[]>(
    () => this.grupos().find((g) => g.sala === this.salaElegida())?.docentes ?? []
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

  elegirSala(sala: string): void {
    this.salaElegida.set(sala);
  }

  volverASalas(): void {
    this.salaElegida.set(null);
  }

  elegir(docente: Docente): void {
    this.router.navigate(['/reservar', docente.id]);
  }

  // Iniciales para el avatar: primera letra de la primera y la última palabra del nombre.
  // Ej: "Ana Gómez" -> "AG". Si solo hay una palabra, repite esa letra.
  iniciales(nombreCompleto: string): string {
    const partes = nombreCompleto.trim().split(/\s+/);
    const primera = partes[0]?.[0] ?? '';
    const ultima = partes.length > 1 ? partes[partes.length - 1][0] : primera;
    return (primera + ultima).toUpperCase();
  }

  // Color de avatar según el id del docente, para que cada uno tenga siempre el mismo color.
  colorAvatar(docenteId: number): string {
    return COLORES_AVATAR[docenteId % COLORES_AVATAR.length];
  }

  private esSala(sala: string | null): boolean {
    return !!sala && sala.trim().toLowerCase().startsWith('sala ');
  }
}






/*import { Component, OnInit, signal } from '@angular/core';
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
  //
  elegir(docente: Docente): void {
    this.router.navigate(['/reservar', docente.id]);
  }
}*/
