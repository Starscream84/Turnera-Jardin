export type TeacherCycle = 'FirstCycle' | 'SecondCycle' | 'Specialist';

export interface Teacher {
  id: number;
  fullName: string;
  classroom: string | null;
  /** null = no cycle: the family chooses in-person or online. */
  cycle: TeacherCycle | null;
}

export interface TeacherAdmin {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  classroom: string | null;
  cycle: TeacherCycle | null;
  isActive: boolean;
}

export interface TeacherFormValue {
  firstName: string;
  lastName: string;
  email: string;
  classroom: string | null;
  cycle: TeacherCycle | null;
  isActive: boolean;
}
