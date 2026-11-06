import type { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

const ALLOWED = /^[0-9+\s\-().]+$/;

/**
 * Same logic as PhoneNumber.Normalize in the backend (Services/PhoneNumber.cs): keep both in sync.
 * Returns the number in E.164 without "+" (for Argentina: 549 + area code + number), or null if it
 * is not a valid phone. Accepts "2235551234", "0223 15 555-1234", "(223) 555-1234", "+54 9 223 555-1234"...
 * A number from abroad must start with "+" (or "00").
 */
export function normalizePhone(raw: string | null | undefined): string | null {
  const text = (raw ?? '').trim();
  if (!text || !ALLOWED.test(text)) return null;

  let international = text.startsWith('+');
  let digits = text.replace(/\D/g, '');

  if (digits.startsWith('00')) {
    digits = digits.slice(2);
    international = true;
  }

  let national: string;
  if (digits.startsWith('54') && (international || digits.length >= 12)) {
    national = digits.slice(2);
    if (national.startsWith('9')) national = national.slice(1);
  } else if (international) {
    return digits.length >= 8 && digits.length <= 15 ? digits : null;
  } else {
    national = digits;
  }

  if (national.startsWith('0')) national = national.slice(1);

  // Mobile written the old way: area code + "15" + number. The "15" is dropped.
  if (national.length === 12) {
    for (let areaLength = 2; areaLength <= 4; areaLength++) {
      if (national.substring(areaLength, areaLength + 2) === '15') {
        national = national.slice(0, areaLength) + national.slice(areaLength + 2);
        break;
      }
    }
  }

  return national.length === 10 ? `549${national}` : null;
}

/** Form validator: { phone: true } if the value can't be normalized. Empty values are left to Validators.required. */
export const phoneValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = control.value as string | null;
  if (!value) return null;
  return normalizePhone(value) ? null : { phone: true };
};

/**
 * Same rule as StrictEmailAttribute in the backend: the domain must have a dot and an extension
 * (Angular's Validators.email accepts "juan@gmail").
 */
export const STRICT_EMAIL_PATTERN = /^[^@\s]+@[^@\s.]+(\.[^@\s.]+)*\.[A-Za-z]{2,}$/;
