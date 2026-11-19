import { FormControl } from '@angular/forms';
import { fullNameValidator } from './name.util';

describe('fullNameValidator', () => {
  it.each(['Ana Pérez', 'María José Gómez', '  Tomás   Ruiz  ', 'Juan de la Cruz'])('acepta "%s"', (value) => {
    expect(fullNameValidator(new FormControl(value))).toBeNull();
  });

  it.each(['Ana', '  Ana  ', 'A B'])('rechaza "%s" (falta el apellido)', (value) => {
    expect(fullNameValidator(new FormControl(value))).toEqual({ fullName: true });
  });

  it('deja el campo vacío para Validators.required', () => {
    expect(fullNameValidator(new FormControl(''))).toBeNull();
  });
});
