export type UserRole = 'Admin' | 'Teacher' | 'Coordinator';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  fullName: string;
  role: UserRole;
  teacherId: number | null;
  photoUrl: string | null;
}
