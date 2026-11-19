import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { TimeoutError } from 'rxjs';
import { errorCode, errorMessage, requestId } from './http-error.util';

function apiError(status: number, body: unknown, headers?: Record<string, string>): HttpErrorResponse {
  return new HttpErrorResponse({ status, error: body, headers: new HttpHeaders(headers ?? {}) });
}

describe('errorCode', () => {
  it('lee el código del cuerpo', () => {
    expect(errorCode(apiError(409, { code: 'APPOINTMENT_TAKEN', message: 'x' }))).toBe('APPOINTMENT_TAKEN');
  });

  it('null si no hay código o no es un error HTTP', () => {
    expect(errorCode(apiError(500, 'texto'))).toBeNull();
    expect(errorCode(new Error('x'))).toBeNull();
  });
});

describe('requestId', () => {
  it('prefiere el del cuerpo', () => {
    expect(requestId(apiError(500, { requestId: 'abc' }, { 'X-Request-Id': 'def' }))).toBe('abc');
  });

  it('si no, usa el header', () => {
    expect(requestId(apiError(500, null, { 'X-Request-Id': 'def' }))).toBe('def');
  });
});

describe('errorMessage', () => {
  const fallback = 'No se pudo.';

  it('timeout', () => {
    expect(errorMessage(new TimeoutError(), fallback)).toContain('tardando');
  });

  it('sin conexión', () => {
    expect(errorMessage(apiError(0, null), fallback)).toContain('No pudimos conectarnos');
  });

  it('429 muestra el mensaje del backend (bloqueo del usuario)', () => {
    const err = apiError(429, { code: 'ACCOUNT_LOCKED', message: 'Demasiados intentos fallidos con este usuario.' });
    expect(errorMessage(err, fallback)).toBe('Demasiados intentos fallidos con este usuario.');
  });

  it('429 sin cuerpo usa el texto genérico', () => {
    expect(errorMessage(apiError(429, null), fallback)).toContain('muchos intentos');
  });

  it('5xx no muestra el detalle interno pero sí el código del pedido', () => {
    const msg = errorMessage(apiError(500, { message: 'NullReferenceException', requestId: 'r1' }), fallback);
    expect(msg).toContain('código r1');
    expect(msg).not.toContain('NullReference');
  });

  it('4xx usa el message del backend', () => {
    expect(errorMessage(apiError(409, { message: 'El turno ya fue reservado.' }), fallback)).toBe('El turno ya fue reservado.');
  });

  it('validación sin message: primer error de la lista', () => {
    const err = apiError(400, { errors: { Phone: ['Teléfono inválido.'], Email: ['Email inválido.'] } });
    expect(errorMessage(err, fallback)).toBe('Teléfono inválido.');
  });

  it('cualquier otra cosa: fallback', () => {
    expect(errorMessage(apiError(404, 'Not Found'), fallback)).toBe(fallback);
    expect(errorMessage('raro', fallback)).toBe(fallback);
  });
});
