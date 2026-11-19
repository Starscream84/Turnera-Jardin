import { avatarColor, initials } from './avatar.util';

describe('initials', () => {
  it.each([
    ['Ana Gómez', 'AG'],
    ['ana maría gómez', 'AM'],
    ['  Lucía  ', 'L'],
    ['', ''],
  ])('"%s" → "%s"', (name, expected) => {
    expect(initials(name)).toBe(expected);
  });

  it('acepta null', () => {
    expect(initials(null)).toBe('');
  });
});

describe('avatarColor', () => {
  it('es estable para el mismo id', () => {
    expect(avatarColor(7)).toBe(avatarColor(7));
  });

  it('funciona con ids negativos y grandes', () => {
    expect(avatarColor(-3)).toMatch(/^#[0-9a-f]{6}$/);
    expect(avatarColor(1_000_003)).toMatch(/^#[0-9a-f]{6}$/);
  });
});
