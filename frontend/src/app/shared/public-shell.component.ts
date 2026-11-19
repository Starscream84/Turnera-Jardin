import { Component, Input, ChangeDetectionStrategy } from "@angular/core";
import { RouterLink, RouterLinkActive } from "@angular/router";
import { SCHOOL_PHONES, whatsAppUrl } from "../core/config/school-contact";

/**
 * Visual frame of the public screens (the ones families see): navigation bar,
 * green header with the title, step indicator, contact band and footer.
 * Each screen puts its content inside: <app-public-shell heading="Entrevistas"> ... </app-public-shell>
 */
@Component({
    selector: "app-public-shell",
    standalone: true,
    imports: [RouterLink, RouterLinkActive],
    changeDetection: ChangeDetectionStrategy.Eager,
    template: `
        <header class="hero" [class.hero-compact]="!subtitle">
            <nav class="floating-nav" aria-label="Principal">
                <a routerLink="/reservar" class="logo" aria-label="Colegio IDRA — inicio de entrevistas">
                    <span class="logo-idra">IDRA</span>
                    <span class="logo-school">COLEGIO</span>
                </a>
                <div class="nav-links">
                    <a routerLink="/reservar" routerLinkActive="active" class="nav-link"
                        >Reservar entrevista</a
                    >
                    <a routerLink="/cancelar" routerLinkActive="active" class="nav-link"
                        >Cancelar entrevista</a
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
            <div class="contact-band-actions">
                @for (contact of phones; track contact.label) {
                    <a class="btn btn-green" [href]="whatsAppUrl(contact.phone)" target="_blank" rel="noopener"
                        >WhatsApp {{ contact.label.toLowerCase() }} ›</a
                    >
                }
            </div>
        </section>

        <footer class="footer">
            <div>
                <h3>Contacto</h3>
                <ul class="footer-phones">
                    @for (contact of phones; track contact.label) {
                        <li>
                            <span>{{ contact.label }}:</span>
                            <a [href]="whatsAppUrl(contact.phone)" target="_blank" rel="noopener">{{ contact.phone }}</a>
                        </li>
                    }
                </ul>
            </div>
            <div>
                <h3>Links útiles</h3>
                <div class="footer-links">
                    <a routerLink="/cancelar">Cancelar una entrevista</a>
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

    /** Kindergarten phones (one per shift). They are configured in core/config/school-contact.ts. */
    readonly phones = SCHOOL_PHONES;
    readonly whatsAppUrl = whatsAppUrl;
}
