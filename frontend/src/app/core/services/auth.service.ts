import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ChangePasswordRequest } from '../models/user.model';
import { LoginRequest, LoginResponse } from '../models/auth.model';

// The key changed with the English refactor ("sesion" -> "session"): sessions stored with the old
// shape are ignored, so everyone logs in again and gets a token with the new role names.
const STORAGE_KEY = 'turnera_jardin_session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private session = signal<LoginResponse | null>(this.readFromStorage());

  readonly isLoggedIn = computed(() => this.session() !== null);
  readonly userName = computed(() => this.session()?.fullName ?? '');
  readonly role = computed(() => this.session()?.role ?? null);
  readonly photoUrl = computed(() => this.session()?.photoUrl ?? null);
  /** The photo ready to use in an <img src>, with the backend root prepended. Null if there is none. */
  readonly fullPhotoUrl = computed(() => {
    const relative = this.photoUrl();
    return relative ? `${environment.filesUrl}${relative}` : null;
  });

  constructor(private http: HttpClient) {}

  login(data: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, data).pipe(
      tap((response) => {
        this.session.set(response);
        localStorage.setItem(STORAGE_KEY, JSON.stringify(response));
      })
    );
  }

  logout(): void {
    this.session.set(null);
    localStorage.removeItem(STORAGE_KEY);
  }

  /** Changes the logged-in user's password (management or teacher). Does not change the active session. */
  changePassword(data: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/auth/change-password`, data);
  }

  /** Uploads (or replaces) the logged-in user's profile photo, and updates the active session with the new URL. */
  uploadPhoto(file: File): Observable<{ photoUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<{ photoUrl: string }>(`${environment.apiUrl}/auth/my-photo`, formData).pipe(
      tap((response) => {
        const currentSession = this.session();
        if (currentSession) {
          const updatedSession = { ...currentSession, photoUrl: response.photoUrl };
          this.session.set(updatedSession);
          localStorage.setItem(STORAGE_KEY, JSON.stringify(updatedSession));
        }
      })
    );
  }

  get token(): string | null {
    return this.session()?.token ?? null;
  }

  private readFromStorage(): LoginResponse | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    try {
      const session: LoginResponse = JSON.parse(raw);
      if (new Date(session.expiresAtUtc) <= new Date()) {
        localStorage.removeItem(STORAGE_KEY);
        return null;
      }
      return session;
    } catch {
      return null;
    }
  }
}
