import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../../environments/environment';
import { SESSION_KEY, fakeSession, storeSession } from '../testing/session';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let http: HttpTestingController;

  function setup(): AuthService {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
    return TestBed.inject(AuthService);
  }

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('sin sesión guardada no está logueado', () => {
    storeSession(null);
    const auth = setup();
    expect(auth.isLoggedIn()).toBe(false);
    expect(auth.token).toBeNull();
  });

  it('recupera la sesión guardada', () => {
    storeSession(fakeSession({ role: 'Admin' }));
    const auth = setup();
    expect(auth.isLoggedIn()).toBe(true);
    expect(auth.role()).toBe('Admin');
    expect(auth.token).toBe('token-de-prueba');
  });

  it('descarta una sesión vencida', () => {
    storeSession(fakeSession({ expiresAtUtc: new Date(Date.now() - 1000).toISOString() }));
    const auth = setup();
    expect(auth.isLoggedIn()).toBe(false);
    expect(localStorage.getItem(SESSION_KEY)).toBeNull();
  });

  it('descarta un JSON roto', () => {
    localStorage.setItem(SESSION_KEY, '{roto');
    expect(setup().isLoggedIn()).toBe(false);
  });

  it('login guarda la sesión', () => {
    storeSession(null);
    const auth = setup();
    auth.login({ email: 'a@b.com', password: 'x' }).subscribe();
    http.expectOne(`${environment.apiUrl}/auth/login`).flush(fakeSession({ mustChangePassword: true }));

    expect(auth.isLoggedIn()).toBe(true);
    expect(auth.mustChangePassword()).toBe(true);
    expect(JSON.parse(localStorage.getItem(SESSION_KEY)!).token).toBe('token-de-prueba');
  });

  it('cambiar la contraseña reemplaza la sesión y limpia la marca de temporal', () => {
    storeSession(fakeSession({ mustChangePassword: true }));
    const auth = setup();
    auth.changePassword({ currentPassword: 'a', newPassword: 'b' }).subscribe();
    http.expectOne(`${environment.apiUrl}/auth/change-password`).flush(fakeSession({ token: 'nuevo' }));

    expect(auth.token).toBe('nuevo');
    expect(auth.mustChangePassword()).toBe(false);
  });

  it('markPasswordChangeRequired marca la sesión y la persiste', () => {
    storeSession(fakeSession());
    const auth = setup();
    auth.markPasswordChangeRequired();
    expect(auth.mustChangePassword()).toBe(true);
    expect(JSON.parse(localStorage.getItem(SESSION_KEY)!).mustChangePassword).toBe(true);
  });

  it('una sesión guardada sin el campo nuevo cuenta como contraseña normal', () => {
    const { mustChangePassword: _omit, ...old } = fakeSession();
    localStorage.setItem(SESSION_KEY, JSON.stringify(old));
    expect(setup().mustChangePassword()).toBe(false);
  });

  it('logout borra todo', () => {
    storeSession(fakeSession());
    const auth = setup();
    auth.logout();
    expect(auth.isLoggedIn()).toBe(false);
    expect(localStorage.getItem(SESSION_KEY)).toBeNull();
  });

  it('subir foto actualiza la URL completa', () => {
    storeSession(fakeSession());
    const auth = setup();
    auth.uploadPhoto(new File(['x'], 'f.jpg')).subscribe();
    http.expectOne(`${environment.apiUrl}/auth/my-photo`).flush({ photoUrl: '/fotos-perfil/1.jpg' });
    expect(auth.fullPhotoUrl()).toBe(`${environment.filesUrl}/fotos-perfil/1.jpg`);
  });
});
