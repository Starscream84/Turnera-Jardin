import { FormControl } from '@angular/forms';
import { STRICT_EMAIL_PATTERN, normalizePhone, phoneValidator } from './phone.util';

// Same cases as backend/TurneraJardin.Tests/PhoneNumberTests.cs: both implementations must agree.
describe('normalizePhone', () => {
  it.each([
    ['2235551234', '5492235551234'],
    ['+54 9 223 555-1234', '5492235551234'],
    ['+54 223 555 1234', '5492235551234'],
    ['5492235551234', '5492235551234'],
    ['00 54 9 223 555 1234', '5492235551234'],
    ['0223 555-1234', '5492235551234'],
    ['(223) 555-1234', '5492235551234'],
    ['0223 15 555-1234', '5492235551234'],
    ['223 15 555 1234', '5492235551234'],
    ['011 15 4444-5555', '5491144445555'],
    ['11 4444 5555', '5491144445555'],
    ['2291 15 41-2345', '5492291412345'],
    ['+1 415 555 2671', '14155552671'],
  ])('normaliza %s', (input, expected) => {
    expect(normalizePhone(input)).toBe(expected);
  });

  it.each(['1', '(((1)))', '--------', '        ', '', '555-1234', '223 555 12345678', '٢٢٣٥٥٥١٢٣٤', '223-555-ABCD'])(
    'rechaza "%s"',
    (input) => {
      expect(normalizePhone(input)).toBeNull();
    },
  );

  it('acepta null y undefined', () => {
    expect(normalizePhone(null)).toBeNull();
    expect(normalizePhone(undefined)).toBeNull();
  });

  it('escrito distinto, mismo número', () => {
    expect(normalizePhone('+54 9 223 555-1234')).toBe(normalizePhone('0223 15 555-1234'));
  });
});

describe('phoneValidator', () => {
  it('deja vacío a Validators.required', () => {
    expect(phoneValidator(new FormControl(''))).toBeNull();
  });

  it('marca un teléfono inválido', () => {
    expect(phoneValidator(new FormControl('123'))).toEqual({ phone: true });
  });

  it('acepta un teléfono válido', () => {
    expect(phoneValidator(new FormControl('223 555 1234'))).toBeNull();
  });
});

describe('STRICT_EMAIL_PATTERN', () => {
  it.each(['juan@gmail.com', 'a.b@colegio.edu.ar', 'x+y@dominio.co'])('acepta %s', (email) => {
    expect(STRICT_EMAIL_PATTERN.test(email)).toBe(true);
  });

  it.each(['juan@gmail', 'juan@', '@gmail.com', 'juan gmail.com', 'juan@gmail.c', 'juan@@gmail.com'])('rechaza %s', (email) => {
    expect(STRICT_EMAIL_PATTERN.test(email)).toBe(false);
  });
});
