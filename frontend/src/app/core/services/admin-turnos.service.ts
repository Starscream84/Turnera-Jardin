import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { EstadoTurno, TurnoAdmin } from '../models/turno.model';

export interface FiltroTurnosAdmin {
  docenteId?: number;
  desde?: string;
  hasta?: string;
  estado?: EstadoTurno;
}

@Injectable({ providedIn: 'root' })
export class AdminTurnosService {
  private base = `${environment.apiUrl}/admin/turnos`;

  constructor(private http: HttpClient) {}

  listar(filtro: FiltroTurnosAdmin): Observable<TurnoAdmin[]> {
    let params = new HttpParams();
    if (filtro.docenteId) params = params.set('docenteId', filtro.docenteId);
    if (filtro.desde) params = params.set('desde', filtro.desde);
    if (filtro.hasta) params = params.set('hasta', filtro.hasta);
    if (filtro.estado) params = params.set('estado', filtro.estado);

    return this.http.get<TurnoAdmin[]>(this.base, { params });
  }

  cancelar(id: number): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/cancelar`, {});
  }

  eliminar(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
