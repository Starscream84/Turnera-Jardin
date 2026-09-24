export type Rol = 'Admin' | 'Docente' | 'Coordinador';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiraUtc: string;
  nombreCompleto: string;
  rol: Rol;
  docenteId: number | null;
}
