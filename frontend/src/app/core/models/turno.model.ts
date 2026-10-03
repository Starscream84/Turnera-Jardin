export interface TurnoDisponible {
  id: number;
  fecha: string; // "yyyy-MM-dd"
  horaInicio: string; // "HH:mm:ss"
  horaFin: string;
}

export type ModalidadEntrevista = 'Presencial' | 'Virtual';

export interface ReservaTurno {
  nombrePadre: string;
  telefonoPadre: string;
  nombreNino: string;
  observaciones?: string;
  modalidad: ModalidadEntrevista;
}

export interface TurnoConfirmado {
  id: number;
  docenteNombre: string;
  fecha: string;
  horaInicio: string;
  horaFin: string;
  nombreNino: string;
  modalidad: ModalidadEntrevista;
}

export type EstadoTurno = 'Disponible' | 'Reservado' | 'Cancelado' | 'Completado';

export interface TurnoAdmin {
  id: number;
  docenteId: number;
  docenteNombre: string;
  fecha: string;
  horaInicio: string;
  horaFin: string;
  estado: EstadoTurno;
  nombrePadre: string | null;
  telefonoPadre: string | null;
  nombreNino: string | null;
  observaciones: string | null;
  confirmacionEnviada: boolean;
  recordatorioEnviado: boolean;
  /** null mientras el turno está disponible (todavía nadie eligió). */
  modalidad: ModalidadEntrevista | null;
  /** Fecha/hora (UTC, ISO) en que se le pidió a la familia que confirme por WhatsApp. null si todavía no. */
  confirmacionSolicitada: string | null;
}

export interface GenerarTurnos {
  fechaDesde: string;
  fechaHasta: string;
  diasSemana: number[]; // 0=Domingo .. 6=Sábado
  horaInicio: string; // "HH:mm"
  horaFin: string;
  duracionMinutos: number;
}
