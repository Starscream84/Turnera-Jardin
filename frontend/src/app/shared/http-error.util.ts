import { HttpErrorResponse } from '@angular/common/http';
import { TimeoutError } from 'rxjs';

/** How long the critical public calls (book, cancel) wait before giving up. */
export const REQUEST_TIMEOUT_MS = 20_000;

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
      return 'Hiciste muchos intentos seguidos. Esperá unos minutos y probá de nuevo.';
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
