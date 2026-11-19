import { LoginResponse, UserRole } from '../models/auth.model';

/** Storage key used by AuthService. */
export const SESSION_KEY = 'turnera_jardin_session';

/** Session as the backend returns it, valid for one hour. */
export function fakeSession(overrides: Partial<LoginResponse> = {}): LoginResponse {
  return {
    token: 'token-de-prueba',
    expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(),
    fullName: 'Ana Gómez',
    role: 'Teacher' as UserRole,
    teacherId: 1,
    photoUrl: null,
    mustChangePassword: false,
    ...overrides,
  };
}

/** Saves a session where AuthService reads it at startup (call before TestBed.inject). */
export function storeSession(session: LoginResponse | null): void {
  localStorage.clear();
  if (session) localStorage.setItem(SESSION_KEY, JSON.stringify(session));
}
