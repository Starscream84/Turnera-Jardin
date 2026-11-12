import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isLoggedIn()) {
    return true;
  }

  return router.createUrlTree(['/admin/login']);
};

/**
 * H30: while the password is temporary, every panel screen except "Mi cuenta" redirects there.
 * The backend enforces the same rule (403 PASSWORD_CHANGE_REQUIRED); this only avoids showing
 * screens that would fail.
 */
export const passwordChangeGuard: CanActivateChildFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.mustChangePassword() && !state.url.startsWith('/admin/mi-cuenta')) {
    return router.createUrlTree(['/admin/mi-cuenta']);
  }

  return true;
};
