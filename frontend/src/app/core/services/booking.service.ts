import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, timeout } from 'rxjs';
import { REQUEST_TIMEOUT_MS } from '../../shared/http-error.util';
import { environment } from '../../../environments/environment';
import { Teacher } from '../models/teacher.model';
import { AvailableAppointment, BookAppointmentRequest, BookedAppointment } from '../models/appointment.model';

/**
 * How far ahead a family can book (180 ≈ 6 months). Only used for texts: the dates themselves are
 * decided by the backend with the server's Argentina date (ArgentinaDate.BookingDaysAhead), so a phone
 * with the wrong date or time zone sees the same slots as everyone else. Keep both numbers equal.
 */
export const BOOKING_DAYS_AHEAD = 180;

/** Public endpoints, no login: choose a teacher, see free times and book. */
@Injectable({ providedIn: 'root' })
export class BookingService {
  constructor(private http: HttpClient) {}

  listTeachers(): Observable<Teacher[]> {
    return this.http.get<Teacher[]>(`${environment.apiUrl}/teachers`);
  }

  /** Free appointments of a teacher, for the whole bookable window (from tomorrow, decided by the server). */
  availableAppointments(teacherId: number): Observable<AvailableAppointment[]> {
    return this.http.get<AvailableAppointment[]>(`${environment.apiUrl}/teachers/${teacherId}/available-appointments`);
  }

  /** One free appointment (booking form). 404 if it was booked meanwhile or is no longer bookable. */
  getAppointment(appointmentId: number): Observable<AvailableAppointment> {
    return this.http.get<AvailableAppointment>(`${environment.apiUrl}/appointments/${appointmentId}`);
  }

  bookAppointment(appointmentId: number, data: BookAppointmentRequest): Observable<BookedAppointment> {
    return this.http
      .post<BookedAppointment>(`${environment.apiUrl}/appointments/${appointmentId}/book`, data)
      .pipe(timeout(REQUEST_TIMEOUT_MS));
  }

  cancelAppointment(appointmentId: number, phone: string): Observable<void> {
    // The phone goes in the body, not in the URL (URLs end up in proxy and server logs).
    return this.http
      .post<void>(`${environment.apiUrl}/appointments/${appointmentId}/cancel`, { phone })
      .pipe(timeout(REQUEST_TIMEOUT_MS));
  }
}
