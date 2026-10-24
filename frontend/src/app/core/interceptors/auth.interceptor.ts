import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/** Adds the Authorization header with the JWT to requests that require a login (/admin/..., /auth/change-password and /auth/my-photo). */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token;

  const requiresAuth =
    req.url.includes('/admin/') ||
    req.url.includes('/auth/change-password') ||
    req.url.includes('/auth/my-photo');

  if (token && requiresAuth) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
  }

  return next(req);
};
