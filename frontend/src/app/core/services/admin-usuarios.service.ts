import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UsuarioAdmin, UsuarioCreateRequest, UsuarioCredenciales } from '../models/usuario.model';

@Injectable({ providedIn: 'root' })
export class AdminUsuariosService {
  private base = `${environment.apiUrl}/admin/usuarios`;

  constructor(private http: HttpClient) {}

  listar(): Observable<UsuarioAdmin[]> {
    return this.http.get<UsuarioAdmin[]>(this.base);
  }

  crear(datos: UsuarioCreateRequest): Observable<UsuarioCredenciales> {
    return this.http.post<UsuarioCredenciales>(this.base, datos);
  }

  restablecerPassword(id: number, nuevaPassword?: string): Observable<UsuarioCredenciales> {
    return this.http.post<UsuarioCredenciales>(`${this.base}/${id}/restablecer-password`, {
      nuevaPassword: nuevaPassword || null
    });
  }

  activar(id: number): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}/activar`, {});
  }

  desactivar(id: number): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/desactivar`, {});
  }
}
