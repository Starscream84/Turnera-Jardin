import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';

/** API paths (after environment.apiUrl) that need the logged-in user's token. */
const AUTHENTICATED_PREFIXES = ['/admin/', '/auth/change-password', '/auth/my-photo'];

/**
 * Adds the Authorization header with the JWT only to our own API's private endpoints
 * (the URL must start with environment.apiUrl, so the token is never sent to another site).
 * If one of those requests answers 401 (token expired, password changed, user deactivated),
 * the session is closed and the user is taken to the login screen.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const token = auth.token;

  const isOwnApi = req.url.startsWith(`${environment.apiUrl}/`);
  const path = isOwnApi ? req.url.slice(environment.apiUrl.length) : '';
  const requiresAuth = isOwnApi && AUTHENTICATED_PREFIXES.some((prefix) => path.startsWith(prefix));

  if (!requiresAuth) {
    return next(req);
  }

  const authorized = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authorized).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse && err.status === 401) {
        auth.logout();
        router.navigate(['/admin/login'], { queryParams: { sesion: 'vencida' } });
      }
      return throwError(() => err);
    })
  );
};
