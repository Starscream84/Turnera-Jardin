import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/** Agrega el header Authorization con el JWT a los pedidos que requieren estar logueado (/admin/... y /auth/cambiar-password). */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token;

  const requiereAuth = req.url.includes('/admin/') || req.url.includes('/auth/cambiar-password');

  if (token && requiereAuth) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
  }

  return next(req);
};
