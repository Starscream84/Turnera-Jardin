/** Colores de la paleta IDRA para los avatares con iniciales (cuando no hay foto). */
const COLORES_AVATAR = ['#6f9a3a', '#b7418b', '#d9834a', '#1a3a74', '#4b2c6e', '#c4577a', '#2e6ba8', '#5e7f45'];

/** "Ana Gómez" -> "AG". Toma como mucho las dos primeras palabras. */
export function iniciales(nombre: string | null | undefined): string {
  return (nombre ?? '')
    .trim()
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((palabra) => palabra.charAt(0).toUpperCase())
    .join('');
}

/** Color estable por id: el mismo docente siempre aparece con el mismo color. */
export function colorAvatar(id: number): string {
  return COLORES_AVATAR[Math.abs(id) % COLORES_AVATAR.length];
}
