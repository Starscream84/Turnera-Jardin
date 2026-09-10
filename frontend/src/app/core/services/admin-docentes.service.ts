import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DocenteAdmin, DocenteFormValue } from '../models/docente.model';
import { GenerarTurnos } from '../models/turno.model';

@Injectable({ providedIn: 'root' })
export class AdminDocentesService {
  private base = `${environment.apiUrl}/admin/docentes`;

  constructor(private http: HttpClient) {}

  listar(): Observable<DocenteAdmin[]> {
    return this.http.get<DocenteAdmin[]>(this.base);
  }

  crear(datos: DocenteFormValue): Observable<DocenteAdmin> {
    return this.http.post<DocenteAdmin>(this.base, datos);
  }

  actualizar(id: number, datos: DocenteFormValue): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, datos);
  }

  desactivar(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  generarTurnos(docenteId: number, datos: GenerarTurnos): Observable<{ creados: number }> {
    return this.http.post<{ creados: number }>(`${this.base}/${docenteId}/generar-turnos`, datos);
  }
}
