export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiraUtc: string;
  nombreCompleto: string;
  rol: 'Admin' | 'Docente';
  docenteId: number | null;
}
