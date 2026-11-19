import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Form validator for "Nombre y apellido" fields (a single field with the full name):
 * { fullName: true } unless there are at least two words of two or more letters.
 * Empty values are left to Validators.required.
 */
export const fullNameValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = (control.value as string | null)?.trim();
  if (!value) return null;
  const words = value.split(/\s+/).filter((word) => word.length >= 2);
  return words.length >= 2 ? null : { fullName: true };
};
