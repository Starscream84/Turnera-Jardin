export interface UsuarioAdmin {
  id: number;
  email: string;
  nombreCompleto: string;
  rol: 'Admin' | 'Docente';
  docenteId: number | null;
  docenteNombre: string | null;
  activo: boolean;
}

export interface UsuarioCreateRequest {
  docenteId?: number | null;
  email?: string | null;
  nombreCompleto?: string | null;
  password?: string | null;
}

/** Respuesta al crear un usuario o restablecer su contraseña: trae la contraseña en texto plano, una única vez. */
export interface UsuarioCredenciales {
  id: number;
  email: string;
  nombreCompleto: string;
  rol: string;
  passwordTemporal: string;
}

export interface CambiarPasswordRequest {
  passwordActual: string;
  passwordNueva: string;
}
