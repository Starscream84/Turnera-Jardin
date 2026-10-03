import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Docente } from '../models/docente.model';
import { TurnoDisponible, ReservaTurno, TurnoConfirmado } from '../models/turno.model';

/**
 * Con cuánta anticipación máxima puede reservar una familia: se muestran los turnos libres
 * desde hoy hasta esta cantidad de días para adelante (180 ≈ 6 meses).
 */
export const DIAS_ANTICIPACION_RESERVA = 180;

/** Endpoints públicos, sin login: elegir docente, ver horarios libres y reservar. */
@Injectable({ providedIn: 'root' })
export class DocentesService {
  constructor(private http: HttpClient) {}

  listar(): Observable<Docente[]> {
    return this.http.get<Docente[]>(`${environment.apiUrl}/docentes`);
  }

  /**
   * Turnos libres de un docente desde hoy hasta DIAS_ANTICIPACION_RESERVA días para adelante.
   * Se manda el "hasta" explícito porque, si no, el backend devuelve solo los próximos 30 días.
   */
  turnosDisponibles(docenteId: number): Observable<TurnoDisponible[]> {
    const limite = new Date();
    limite.setDate(limite.getDate() + DIAS_ANTICIPACION_RESERVA);
    const hasta = `${limite.getFullYear()}-${String(limite.getMonth() + 1).padStart(2, '0')}-${String(limite.getDate()).padStart(2, '0')}`;

    return this.http.get<TurnoDisponible[]>(
      `${environment.apiUrl}/docentes/${docenteId}/turnos-disponibles`,
      { params: { hasta } }
    );
  }

  reservarTurno(turnoId: number, datos: ReservaTurno): Observable<TurnoConfirmado> {
    return this.http.post<TurnoConfirmado>(`${environment.apiUrl}/turnos/${turnoId}/reservar`, datos);
  }

  cancelarTurno(turnoId: number, telefono: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/turnos/${turnoId}/cancelar?telefono=${encodeURIComponent(telefono)}`, {});
  }
}
