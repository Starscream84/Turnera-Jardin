import { Component } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { initials } from '../../shared/avatar.util';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './admin-layout.component.html'
})
export class AdminLayoutComponent {
  readonly initials = initials;

  constructor(public auth: AuthService, private router: Router) {}

  roleLabel(): string {
    switch (this.auth.role()) {
      case 'Admin': return 'Administrador';
      case 'Coordinator': return 'Coordinador';
      case 'Teacher': return 'Docente';
      default: return '';
    }
  }

  signOut(): void {
    this.auth.logout();
    this.router.navigate(['/admin/login']);
  }
}
