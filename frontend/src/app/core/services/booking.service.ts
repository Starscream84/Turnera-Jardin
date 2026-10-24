import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Teacher } from '../models/teacher.model';
import { AvailableAppointment, BookAppointmentRequest, BookedAppointment } from '../models/appointment.model';

/**
 * How far ahead a family can book: free appointments are shown from today
 * up to this many days ahead (180 ≈ 6 months).
 */
export const BOOKING_DAYS_AHEAD = 180;

/** Public endpoints, no login: choose a teacher, see free times and book. */
@Injectable({ providedIn: 'root' })
export class BookingService {
  constructor(private http: HttpClient) {}

  listTeachers(): Observable<Teacher[]> {
    return this.http.get<Teacher[]>(`${environment.apiUrl}/teachers`);
  }

  /**
   * Free appointments of a teacher from today up to BOOKING_DAYS_AHEAD days ahead.
   * The "to" date is sent explicitly because otherwise the backend returns only the next 30 days.
   */
  availableAppointments(teacherId: number): Observable<AvailableAppointment[]> {
    const limit = new Date();
    limit.setDate(limit.getDate() + BOOKING_DAYS_AHEAD);
    const to = `${limit.getFullYear()}-${String(limit.getMonth() + 1).padStart(2, '0')}-${String(limit.getDate()).padStart(2, '0')}`;

    return this.http.get<AvailableAppointment[]>(
      `${environment.apiUrl}/teachers/${teacherId}/available-appointments`,
      { params: { to } }
    );
  }

  bookAppointment(appointmentId: number, data: BookAppointmentRequest): Observable<BookedAppointment> {
    return this.http.post<BookedAppointment>(`${environment.apiUrl}/appointments/${appointmentId}/book`, data);
  }

  cancelAppointment(appointmentId: number, phone: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/appointments/${appointmentId}/cancel?phone=${encodeURIComponent(phone)}`, {});
  }
}
