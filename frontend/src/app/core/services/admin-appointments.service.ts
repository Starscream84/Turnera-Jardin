import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { map } from 'rxjs';
import { AppointmentStatus, AppointmentAdmin, AppointmentEvent, AppointmentEventType } from '../models/appointment.model';

export interface AdminAppointmentsFilter {
  teacherId?: number;
  from?: string;
  to?: string;
  status?: AppointmentStatus;
}

export interface HistoryFilter {
  teacherId?: number;
  from?: string;
  to?: string;
  type?: AppointmentEventType;
}

/** One page of a list plus the totals of the whole filter (sent by the backend in headers). */
export interface Page<T> {
  items: T[];
  total: number;
  /** Booked appointments in the whole filter (appointments list only). */
  booked: number;
}

/** Page size of the panel lists (the backend accepts up to 2000). */
export const PAGE_SIZE = 500;

@Injectable({ providedIn: 'root' })
export class AdminAppointmentsService {
  private base = `${environment.apiUrl}/admin/appointments`;

  constructor(private http: HttpClient) {}

  /** One page of appointments (page starts at 1). The totals come in X-Total-Count / X-Booked-Count. */
  list(filter: AdminAppointmentsFilter, page = 1, pageSize = PAGE_SIZE): Observable<Page<AppointmentAdmin>> {
    const params = this.toParams(filter).set('page', page).set('pageSize', pageSize);
    return this.http.get<AppointmentAdmin[]>(this.base, { params, observe: 'response' }).pipe(
      map((response) => ({
        items: response.body ?? [],
        total: Number(response.headers.get('X-Total-Count') ?? response.body?.length ?? 0),
        booked: Number(response.headers.get('X-Booked-Count') ?? 0)
      }))
    );
  }

  /** History of bookings, cancellations and confirmation requests, newest first. */
  history(filter: HistoryFilter, page = 1, pageSize = PAGE_SIZE): Observable<Page<AppointmentEvent>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (filter.teacherId) params = params.set('teacherId', filter.teacherId);
    if (filter.from) params = params.set('from', filter.from);
    if (filter.to) params = params.set('to', filter.to);
    if (filter.type) params = params.set('type', filter.type);
    return this.http.get<AppointmentEvent[]>(`${this.base}/history`, { params, observe: 'response' }).pipe(
      map((response) => ({
        items: response.body ?? [],
        total: Number(response.headers.get('X-Total-Count') ?? response.body?.length ?? 0),
        booked: 0
      }))
    );
  }

  cancel(id: number): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/cancel`, {});
  }

  /** Records that the family was asked to confirm (the message itself goes out through wa.me). */
  requestConfirmation(id: number): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/request-confirmation`, {});
  }

  /**
   * Deletes the listed appointments that are still "Available". Booked ones are skipped by the backend
   * (they have to be cancelled one by one, which notifies the family). Cannot be undone.
   */
  deleteMany(ids: number[]): Observable<{ deleted: number; skippedBooked: number }> {
    return this.http.post<{ deleted: number; skippedBooked: number }>(`${this.base}/bulk-delete`, { ids });
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
