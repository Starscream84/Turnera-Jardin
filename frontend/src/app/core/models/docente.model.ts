export type CicloDocente = 'PrimerCiclo' | 'SegundoCiclo' | 'Especialista';

export interface Docente {
  id: number;
  nombreCompleto: string;
  sala: string | null;
  /** null = sin ciclo: la familia elige presencial o virtual. */
  ciclo: CicloDocente | null;
}

export interface DocenteAdmin {
  id: number;
  nombre: string;
  apellido: string;
  email: string;
  sala: string | null;
  ciclo: CicloDocente | null;
  activo: boolean;
}

export interface DocenteFormValue {
  nombre: string;
  apellido: string;
  email: string;
  sala: string | null;
  ciclo: CicloDocente | null;
  activo: boolean;
}
