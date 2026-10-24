import { UserRole } from './auth.model';

export interface UserAdmin {
  id: number;
  email: string;
  fullName: string;
  role: UserRole;
  teacherId: number | null;
  teacherName: string | null;
  isActive: boolean;
  photoUrl: string | null;
}

export interface UserCreateRequest {
  teacherId?: number | null;
  email?: string | null;
  fullName?: string | null;
  password?: string | null;
}

/** Response when creating a user or resetting their password: includes the plain-text password, only once. */
export interface UserCredentials {
  id: number;
  email: string;
  fullName: string;
  role: string;
  temporaryPassword: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
