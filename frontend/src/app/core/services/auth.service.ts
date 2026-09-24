import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CambiarPasswordRequest } from '../models/usuario.model';
import { LoginRequest, LoginResponse } from '../models/auth.model';

const STORAGE_KEY = 'turnera_jardin_sesion';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private sesion = signal<LoginResponse | null>(this.leerDeStorage());

  readonly estaLogueado = computed(() => this.sesion() !== null);
  readonly nombreUsuario = computed(() => this.sesion()?.nombreCompleto ?? '');
  readonly rol = computed(() => this.sesion()?.rol ?? null);

  constructor(private http: HttpClient) {}

  login(datos: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, datos).pipe(
      tap((respuesta) => {
        this.sesion.set(respuesta);
        localStorage.setItem(STORAGE_KEY, JSON.stringify(respuesta));
      })
    );
  }

  logout(): void {
    this.sesion.set(null);
    localStorage.removeItem(STORAGE_KEY);
  }

  /** Cambia la contraseña del usuario logueado (dirección o docente). No cambia la sesión activa. */
  cambiarPassword(datos: CambiarPasswordRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/auth/cambiar-password`, datos);
  }

  get token(): string | null {
    return this.sesion()?.token ?? null;
  }

  private leerDeStorage(): LoginResponse | null {
    const crudo = localStorage.getItem(STORAGE_KEY);
    if (!crudo) return null;
    try {
      const sesion: LoginResponse = JSON.parse(crudo);
      if (new Date(sesion.expiraUtc) <= new Date()) {
        localStorage.removeItem(STORAGE_KEY);
        return null;
      }
      return sesion;
    } catch {
      return null;
    }
  }
}
