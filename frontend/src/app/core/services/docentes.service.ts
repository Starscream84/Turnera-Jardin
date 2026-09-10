import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Docente } from '../models/docente.model';
import { TurnoDisponible, ReservaTurno, TurnoConfirmado } from '../models/turno.model';

/** Endpoints públicos, sin login: elegir docente, ver horarios libres y reservar. */
@Injectable({ providedIn: 'root' })
export class DocentesService {
  constructor(private http: HttpClient) {}

  listar(): Observable<Docente[]> {
    return this.http.get<Docente[]>(`${environment.apiUrl}/docentes`);
  }

  turnosDisponibles(docenteId: number): Observable<TurnoDisponible[]> {
    return this.http.get<TurnoDisponible[]>(`${environment.apiUrl}/docentes/${docenteId}/turnos-disponibles`);
  }

  reservarTurno(turnoId: number, datos: ReservaTurno): Observable<TurnoConfirmado> {
    return this.http.post<TurnoConfirmado>(`${environment.apiUrl}/turnos/${turnoId}/reservar`, datos);
  }

  cancelarTurno(turnoId: number, telefono: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/turnos/${turnoId}/cancelar?telefono=${encodeURIComponent(telefono)}`, {});
  }
}
