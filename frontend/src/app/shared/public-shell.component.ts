import { Component, Input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

/**
 * Marco visual de las pantallas públicas (las que ven las familias): barra de navegación,
 * encabezado verde con el título, indicador de pasos, banda de contacto y pie.
 * Cada pantalla pone su contenido adentro: <app-public-shell titulo="Turnos"> ... </app-public-shell>
 */
@Component({
  selector: 'app-public-shell',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <header class="hero" [class.hero-compacto]="!bajada">
      <nav class="nav-flotante" aria-label="Principal">
        <a routerLink="/reservar" class="logo" aria-label="Colegio IDRA — inicio de turnos">
          <span class="logo-idra">IDRA</span>
          <span class="logo-colegio">COLEGIO</span>
        </a>
        <div class="nav-links">
          <a routerLink="/reservar" routerLinkActive="activo" class="nav-link">Reservar turno</a>
          <a routerLink="/cancelar" routerLinkActive="activo" class="nav-link">Cancelar turno</a>
        </div>
        <a routerLink="/admin/login" class="btn btn-primario">Ingreso docentes</a>
      </nav>

      <h1 class="hero-titulo">{{ titulo }}</h1>
      @if (bajada) {
        <span class="hero-linea" aria-hidden="true"></span>
        <p class="hero-bajada">{{ bajada }}</p>
      }
    </header>

    <main class="seccion" [class.seccion-angosta]="angosto">
      @if (paso) {
        <ol class="pasos" aria-label="Pasos de la reserva">
          @for (nombre of nombresPasos; track nombre; let i = $index) {
            <li [class.hecho]="i + 1 < paso" [class.actual]="i + 1 === paso" [attr.aria-current]="i + 1 === paso ? 'step' : null">
              {{ i + 1 }}. {{ nombre }}
            </li>
          }
        </ol>
      }
      <ng-content />
    </main>

    <section class="banda-contacto">
      <h2>¿Alguna duda? conversemos</h2>
      <a class="btn btn-verde" [href]="enlaceWhatsApp" target="_blank" rel="noopener">Chatear vía WhatsApp ›</a>
    </section>

    <footer class="pie">
      <div>
        <h3>Contacto</h3>
        <a [href]="enlaceWhatsApp" target="_blank" rel="noopener">{{ telefonoVisible }}</a>
      </div>
      <div>
        <h3>Links útiles</h3>
        <div class="pie-links">
          <a routerLink="/cancelar">Cancelar un turno</a>
          <a routerLink="/admin/login">Ingreso docentes</a>
        </div>
      </div>
    </footer>
  `
})
export class PublicShellComponent {
  @Input({ required: true }) titulo = '';
  /** Texto debajo del título. Sin bajada, el encabezado se muestra en versión compacta. */
  @Input() bajada = '';
  /** Paso actual de la reserva (1 a 3). Sin valor, no se muestra el indicador. */
  @Input() paso: number | null = null;
  /** Contenido en columna angosta (formularios cortos). */
  @Input() angosto = false;

  readonly nombresPasos = ['Docente', 'Día y horario', 'Tus datos'];

  // Teléfono de contacto que figura en el sitio del colegio. Cambiar acá si el jardín usa otro número.
  readonly telefonoVisible = '+54 9 223 519-9012';
  readonly enlaceWhatsApp = 'https://wa.me/5492235199012';
}
