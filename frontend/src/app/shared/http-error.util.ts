import { HttpErrorResponse } from '@angular/common/http';
import { TimeoutError } from 'rxjs';

/** How long the critical public calls (book, cancel) wait before giving up. */
export const REQUEST_TIMEOUT_MS = 20_000;

/**
 * Machine-readable code of an API error (e.g. "APPOINTMENT_TAKEN", "PASSWORD_CHANGE_REQUIRED").
 * Every backend error carries one; the list is in docs/migraciones-y-errores.md. Null if there is none.
 */
export function errorCode(err: unknown): string | null {
  if (err instanceof HttpErrorResponse && err.error && typeof err.error === 'object' && typeof err.error.code === 'string') {
    return err.error.code;
  }
  return null;
}

/** Id of the request that failed (X-Request-Id), to find it in the server logs. */
export function requestId(err: unknown): string | null {
  if (!(err instanceof HttpErrorResponse)) return null;
  const fromBody = err.error && typeof err.error === 'object' ? err.error.requestId : null;
  return (typeof fromBody === 'string' && fromBody) || err.headers?.get('X-Request-Id') || null;
}

/**
 * Text to show for a failed request. Uses the backend's own explanation when there is one
 * ({ message } in every API error, including validation errors), and specific texts for
 * "no connection", "timeout" and "too many attempts". `fallback` is used for anything else.
 */
export function errorMessage(err: unknown, fallback: string): string {
  if (err instanceof TimeoutError) {
    return 'El servidor está tardando en responder. Revisá tu conexión y, antes de reintentar, fijate si te llegó el email de confirmación.';
  }

  if (err instanceof HttpErrorResponse) {
    if (err.status === 0) {
      return 'No pudimos conectarnos con el servidor. Revisá tu conexión a internet y probá de nuevo.';
    }
    if (err.status === 429) {
      // ACCOUNT_LOCKED and RATE_LIMITED come with different explanations: show the backend's one.
      const msg = err.error && typeof err.error === 'object' ? err.error.message : null;
      return typeof msg === 'string' && msg
        ? msg
        : 'Hiciste muchos intentos seguidos. Esperá unos minutos y probá de nuevo.';
    }
    if (err.status >= 500) {
      // Unexpected error: the code lets the school find exactly what happened in the logs.
      const id = requestId(err);
      return `Ocurrió un error inesperado. Intentá de nuevo en un momento.${id ? ` (código ${id})` : ''}`;
    }

    const body = err.error;
    if (body && typeof body === 'object') {
      if (typeof body.message === 'string' && body.message) return body.message;
      // ValidationProblemDetails without "message": first error of the list.
      if (body.errors && typeof body.errors === 'object') {
        const first = Object.values(body.errors as Record<string, string[]>).flat()[0];
        if (typeof first === 'string' && first) return first;
      }
    }
  }

  return fallback;
}
