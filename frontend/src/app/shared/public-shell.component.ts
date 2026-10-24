import { Component, Input } from "@angular/core";
import { RouterLink, RouterLinkActive } from "@angular/router";

/**
 * Visual frame of the public screens (the ones families see): navigation bar,
 * green header with the title, step indicator, contact band and footer.
 * Each screen puts its content inside: <app-public-shell heading="Turnos"> ... </app-public-shell>
 */
@Component({
    selector: "app-public-shell",
    standalone: true,
    imports: [RouterLink, RouterLinkActive],
    template: `
        <header class="hero" [class.hero-compact]="!subtitle">
            <nav class="floating-nav" aria-label="Principal">
                <a routerLink="/reservar" class="logo" aria-label="Colegio IDRA — inicio de turnos">
                    <span class="logo-idra">IDRA</span>
                    <span class="logo-school">COLEGIO</span>
                </a>
                <div class="nav-links">
                    <a routerLink="/reservar" routerLinkActive="active" class="nav-link"
                        >Reservar turno</a
                    >
                    <a routerLink="/cancelar" routerLinkActive="active" class="nav-link"
                        >Cancelar turno</a
                    >
                </div>
            </nav>

            <h1 class="hero-title">{{ heading }}</h1>
            @if (subtitle) {
                <span class="hero-line" aria-hidden="true"></span>
                <p class="hero-subtitle">{{ subtitle }}</p>
            }
        </header>

        <main class="section" [class.section-narrow]="narrow">
            @if (step) {
                <ol class="steps" aria-label="Pasos de la reserva">
                    @for (stepName of stepNames; track stepName; let i = $index) {
                        <li
                            [class.done]="i + 1 < step"
                            [class.current]="i + 1 === step"
                            [attr.aria-current]="i + 1 === step ? 'step' : null"
                        >
                            {{ i + 1 }}. {{ stepName }}
                        </li>
                    }
                </ol>
            }
            <ng-content />
        </main>

        <section class="contact-band">
            <h2>¿Alguna duda? conversemos</h2>
            <a class="btn btn-green" [href]="whatsAppLink" target="_blank" rel="noopener"
                >Chatear vía WhatsApp ›</a
            >
        </section>

        <footer class="footer">
            <div>
                <h3>Contacto</h3>
                <a [href]="whatsAppLink" target="_blank" rel="noopener">{{ displayPhone }}</a>
            </div>
            <div>
                <h3>Links útiles</h3>
                <div class="footer-links">
                    <a routerLink="/cancelar">Cancelar un turno</a>
                </div>
            </div>
        </footer>
    `,
})
export class PublicShellComponent {
    @Input({ required: true }) heading = "";
    /** Text below the title. Without a subtitle, the header is shown in its compact version. */
    @Input() subtitle = "";
    /** Current booking step (1 to 3). Without a value, the indicator is not shown. */
    @Input() step: number | null = null;
    /** Content in a narrow column (short forms). */
    @Input() narrow = false;

    readonly stepNames = ["Docente", "Día y horario", "Tus datos"];

    // Contact phone shown on the school's website. Change it here if the kindergarten uses another number.
    readonly displayPhone = "+54 9 223 519-9012";
    readonly whatsAppLink = "https://wa.me/5492235199012";
}
