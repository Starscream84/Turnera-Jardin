import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';
import { fakeSession, storeSession } from '../testing/session';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let router: Router;
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    storeSession(fakeSession());
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  afterEach(() => {
    backend.verify();
    localStorage.clear();
  });

  it('agrega el token a los endpoints privados de la API', () => {
    http.get(`${environment.apiUrl}/admin/appointments`).subscribe();
    const req = backend.expectOne(`${environment.apiUrl}/admin/appointments`);
    expect(req.request.headers.get('Authorization')).toBe('Bearer token-de-prueba');
    req.flush([]);
  });

  it('no manda el token a los endpoints públicos', () => {
    http.get(`${environment.apiUrl}/teachers`).subscribe();
    const req = backend.expectOne(`${environment.apiUrl}/teachers`);
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush([]);
  });

  it('no manda el token a otro sitio aunque la ruta se parezca', () => {
    http.get('https://otro-sitio.com/api/admin/users').subscribe();
    const req = backend.expectOne('https://otro-sitio.com/api/admin/users');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush([]);
  });

  it('ni a un dominio que empieza igual que la API', () => {
    const lookalike = `${environment.apiUrl}.evil.com/admin/users`;
    http.get(lookalike).subscribe();
    const req = backend.expectOne(lookalike);
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush([]);
  });

  it('401 cierra la sesión y lleva al login', () => {
    const auth = TestBed.inject(AuthService);
    http.get(`${environment.apiUrl}/admin/users`).subscribe({ error: () => undefined });
    backend.expectOne(`${environment.apiUrl}/admin/users`).flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(auth.isLoggedIn()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(['/admin/login'], { queryParams: { sesion: 'vencida' } });
  });

  it('403 PASSWORD_CHANGE_REQUIRED marca la sesión y lleva a Mi cuenta', () => {
    const auth = TestBed.inject(AuthService);
    http.get(`${environment.apiUrl}/admin/appointments`).subscribe({ error: () => undefined });
    backend
      .expectOne(`${environment.apiUrl}/admin/appointments`)
      .flush({ code: 'PASSWORD_CHANGE_REQUIRED', message: 'x' }, { status: 403, statusText: 'Forbidden' });

    expect(auth.isLoggedIn()).toBe(true);
    expect(auth.mustChangePassword()).toBe(true);
    expect(navigate).toHaveBeenCalledWith(['/admin/mi-cuenta']);
  });

  it('otro 403 no toca la sesión', () => {
    const auth = TestBed.inject(AuthService);
    http.get(`${environment.apiUrl}/admin/users`).subscribe({ error: () => undefined });
    backend.expectOne(`${environment.apiUrl}/admin/users`).flush({ code: 'FORBIDDEN' }, { status: 403, statusText: 'Forbidden' });

    expect(auth.mustChangePassword()).toBe(false);
    expect(navigate).not.toHaveBeenCalled();
  });
});
