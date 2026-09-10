export interface TurnoDisponible {
  id: number;
  fecha: string; // "yyyy-MM-dd"
  horaInicio: string; // "HH:mm:ss"
  horaFin: string;
}

export interface ReservaTurno {
  nombrePadre: string;
  telefonoPadre: string;
  nombreNino: string;
  observaciones?: string;
}

export interface TurnoConfirmado {
  id: number;
  docenteNombre: string;
  fecha: string;
  horaInicio: string;
  horaFin: string;
  nombreNino: string;
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
}

export interface GenerarTurnos {
  fechaDesde: string;
  fechaHasta: string;
  diasSemana: number[]; // 0=Domingo .. 6=Sábado
  horaInicio: string; // "HH:mm"
  horaFin: string;
  duracionMinutos: number;
}
