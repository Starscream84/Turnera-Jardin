import { Component } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { iniciales } from '../../shared/avatar.util';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './admin-layout.component.html'
})
export class AdminLayoutComponent {
  readonly iniciales = iniciales;

  constructor(public auth: AuthService, private router: Router) {}

  etiquetaRol(): string {
    switch (this.auth.rol()) {
      case 'Admin': return 'Administrador';
      case 'Coordinador': return 'Coordinador';
      case 'Docente': return 'Docente';
      default: return '';
    }
  }

  salir(): void {
    this.auth.logout();
    this.router.navigate(['/admin/login']);
  }
}
