export interface AvailableAppointment {
  id: number;
  /** The API also sends teacherName, classroom and status; only what the screens use is declared. */
  teacherId: number;
  date: string; // "yyyy-MM-dd"
  startTime: string; // "HH:mm:ss"
  endTime: string;
  /** Address of the interview if it is in person. null for appointments generated before this field existed. */
  location: string | null;
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
  /** Interview address. null if the interview is online. */
  location: string | null;
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
  /** Address of the interview if it is in person (chosen when generating the appointment). */
  location: string | null;
}

export interface GenerateAppointmentsRequest {
  fromDate: string;
  toDate: string;
  daysOfWeek: number[]; // 0 = Sunday .. 6 = Saturday
  startTime: string; // "HH:mm:ss"
  endTime: string;
  durationMinutes: number;
  /** One of the addresses returned by /admin/interview-locations. */
  location: string;
}

/** What happened to an appointment (history). Same names as AppointmentEventType in the backend. */
export type AppointmentEventType = 'Booked' | 'CancelledByFamily' | 'CancelledByStaff' | 'ConfirmationRequested';

export const APPOINTMENT_EVENT_LABELS: Record<AppointmentEventType, string> = {
  Booked: 'Reserva',
  CancelledByFamily: 'Cancelado por la familia',
  CancelledByStaff: 'Cancelado desde el panel',
  ConfirmationRequested: 'Confirmación pedida',
};

/** One row of the history (GET /api/admin/appointments/history). */
export interface AppointmentEvent {
  id: number;
  appointmentId: number;
  teacherId: number;
  teacherName: string;
  date: string;
  startTime: string;
  type: AppointmentEventType;
  mode: InterviewMode | null;
  guardianName: string | null;
  guardianPhone: string | null;
  childName: string | null;
  /** Panel user who did it; null when the family did it from the web. */
  userName: string | null;
  /** UTC */
  createdAt: string;
}
