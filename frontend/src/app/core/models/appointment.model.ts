export interface AvailableAppointment {
  id: number;
  date: string; // "yyyy-MM-dd"
  startTime: string; // "HH:mm:ss"
  endTime: string;
}

export type InterviewMode = 'InPerson' | 'Online';

/** Text shown to families and teachers for each interview mode. */
export const INTERVIEW_MODE_LABELS: Record<InterviewMode, string> = {
  InPerson: 'Presencial',
  Online: 'Virtual',
};

export interface BookAppointmentRequest {
  guardianFirstName: string;
  guardianLastName: string;
  guardianPhone: string;
  guardianEmail: string;
  childFirstName: string;
  childLastName: string;
  notes?: string;
  mode: InterviewMode;
}

export interface BookedAppointment {
  id: number;
  teacherName: string;
  date: string;
  startTime: string;
  endTime: string;
  childFirstName: string;
  childLastName: string;
  mode: InterviewMode;
}

export type AppointmentStatus = 'Available' | 'Booked' | 'Cancelled' | 'Completed';

/** Text shown in the panel for each appointment status. */
export const APPOINTMENT_STATUS_LABELS: Record<AppointmentStatus, string> = {
  Available: 'Disponible',
  Booked: 'Reservado',
  Cancelled: 'Cancelado',
  Completed: 'Completado',
};

export interface AppointmentAdmin {
  id: number;
  teacherId: number;
  teacherName: string;
  date: string;
  startTime: string;
  endTime: string;
  status: AppointmentStatus;
  guardianFirstName: string | null;
  guardianLastName: string | null;
  guardianPhone: string | null;
  childFirstName: string | null;
  childLastName: string | null;
  notes: string | null;
  confirmationSent: boolean;
  reminderSent: boolean;
  /** null while the appointment is available (nobody has chosen yet). */
  mode: InterviewMode | null;
  /** Date/time (UTC, ISO) when the family was asked to confirm via WhatsApp. null if not yet. */
  confirmationRequestedAt: string | null;
  guardianEmail: string | null;
}

export interface GenerateAppointmentsRequest {
  fromDate: string;
  toDate: string;
  daysOfWeek: number[]; // 0 = Sunday .. 6 = Saturday
  startTime: string; // "HH:mm:ss"
  endTime: string;
  durationMinutes: number;
}
