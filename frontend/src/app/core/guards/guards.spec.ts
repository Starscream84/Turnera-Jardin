import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { UserRole } from '../models/auth.model';
import { fakeSession, storeSession } from '../testing/session';
import { adminGuard, coordinatorGuard } from './admin.guard';
import { authGuard, passwordChangeGuard } from './auth.guard';

const route = {} as ActivatedRouteSnapshot;
const state = (url: string) => ({ url }) as RouterStateSnapshot;

function run<T>(guard: (...args: never[]) => T, ...args: unknown[]): T {
  TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient()] });
  return TestBed.runInInjectionContext(() => (guard as (...a: unknown[]) => T)(...args));
}

function urlOf(result: unknown): string | null {
  return result instanceof UrlTree ? TestBed.inject(Router).serializeUrl(result) : null;
}

afterEach(() => localStorage.clear());

describe('authGuard', () => {
  it('sin sesión manda al login', () => {
    storeSession(null);
    expect(urlOf(run(authGuard, route, state('/admin/turnos')))).toBe('/admin/login');
  });

  it('con sesión deja pasar', () => {
    storeSession(fakeSession());
    expect(run(authGuard, route, state('/admin/turnos'))).toBe(true);
  });
});

describe('passwordChangeGuard', () => {
  it('con contraseña temporal manda a Mi cuenta', () => {
    storeSession(fakeSession({ mustChangePassword: true }));
    expect(urlOf(run(passwordChangeGuard, route, state('/admin/turnos')))).toBe('/admin/mi-cuenta');
  });

  it('con contraseña temporal deja entrar a Mi cuenta', () => {
    storeSession(fakeSession({ mustChangePassword: true }));
    expect(run(passwordChangeGuard, route, state('/admin/mi-cuenta'))).toBe(true);
  });

  it('con contraseña normal no interviene', () => {
    storeSession(fakeSession());
    expect(run(passwordChangeGuard, route, state('/admin/usuarios'))).toBe(true);
  });
});

describe('guards por rol', () => {
  const cases: [string, typeof adminGuard, UserRole, boolean][] = [
    ['adminGuard', adminGuard, 'Admin', true],
    ['adminGuard', adminGuard, 'Coordinator', false],
    ['adminGuard', adminGuard, 'Teacher', false],
    ['coordinatorGuard', coordinatorGuard, 'Admin', true],
    ['coordinatorGuard', coordinatorGuard, 'Coordinator', true],
    ['coordinatorGuard', coordinatorGuard, 'Teacher', false],
  ];

  it.each(cases)('%s con rol %s → permitido=%s', (_name, guard, role, allowed) => {
    storeSession(fakeSession({ role }));
    const result = run(guard, route, state('/admin/x'));
    if (allowed) {
      expect(result).toBe(true);
    } else {
      expect(urlOf(result)).toBe('/admin/turnos');
    }
  });

  it('sin sesión tampoco pasa', () => {
    storeSession(null);
    expect(urlOf(run(adminGuard, route, state('/admin/usuarios')))).toBe('/admin/turnos');
  });
});
