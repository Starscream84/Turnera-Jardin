import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AppointmentStatus, AppointmentAdmin } from '../models/appointment.model';

export interface AdminAppointmentsFilter {
  teacherId?: number;
  from?: string;
  to?: string;
  status?: AppointmentStatus;
}

@Injectable({ providedIn: 'root' })
export class AdminAppointmentsService {
  private base = `${environment.apiUrl}/admin/appointments`;

  constructor(private http: HttpClient) {}

  list(filter: AdminAppointmentsFilter): Observable<AppointmentAdmin[]> {
    return this.http.get<AppointmentAdmin[]>(this.base, { params: this.toParams(filter) });
  }

  cancel(id: number): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/cancel`, {});
  }

  /** Records that the family was asked to confirm (the message itself goes out through wa.me). */
  requestConfirmation(id: number): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/request-confirmation`, {});
  }

  /** Bulk-deletes every appointment matching the filter (the backend requires at least one filter). Cannot be undone. */
  deleteMany(filter: AdminAppointmentsFilter): Observable<{ deleted: number }> {
    return this.http.delete<{ deleted: number }>(this.base, { params: this.toParams(filter) });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  private toParams(filter: AdminAppointmentsFilter): HttpParams {
    let params = new HttpParams();
    if (filter.teacherId) params = params.set('teacherId', filter.teacherId);
    if (filter.from) params = params.set('from', filter.from);
    if (filter.to) params = params.set('to', filter.to);
    if (filter.status) params = params.set('status', filter.status);
    return params;
  }
}
