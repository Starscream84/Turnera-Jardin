import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TeacherAdmin, TeacherFormValue } from '../models/teacher.model';
import { GenerateAppointmentsRequest } from '../models/appointment.model';

@Injectable({ providedIn: 'root' })
export class AdminTeachersService {
  private base = `${environment.apiUrl}/admin/teachers`;

  constructor(private http: HttpClient) {}

  list(): Observable<TeacherAdmin[]> {
    return this.http.get<TeacherAdmin[]>(this.base);
  }

  create(data: TeacherFormValue): Observable<TeacherAdmin> {
    return this.http.post<TeacherAdmin>(this.base, data);
  }

  update(id: number, data: TeacherFormValue): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, data);
  }

  deactivate(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  generateAppointments(teacherId: number, data: GenerateAppointmentsRequest): Observable<{ created: number }> {
    return this.http.post<{ created: number }>(`${this.base}/${teacherId}/generate-appointments`, data);
  }
}
