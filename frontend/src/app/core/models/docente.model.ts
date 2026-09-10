export interface Docente {
  id: number;
  nombreCompleto: string;
  sala: string | null;
}

export interface DocenteAdmin {
  id: number;
  nombre: string;
  apellido: string;
  email: string;
  sala: string | null;
  activo: boolean;
}

export interface DocenteFormValue {
  nombre: string;
  apellido: string;
  email: string;
  sala: string | null;
  activo: boolean;
}
