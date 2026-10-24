/** IDRA palette colors for initials avatars (when there is no photo). */
const AVATAR_COLORS = ['#6f9a3a', '#b7418b', '#d9834a', '#1a3a74', '#4b2c6e', '#c4577a', '#2e6ba8', '#5e7f45'];

/** "Ana Gómez" -> "AG". Takes at most the first two words. */
export function initials(name: string | null | undefined): string {
  return (name ?? '')
    .trim()
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((word) => word.charAt(0).toUpperCase())
    .join('');
}

/** Stable color per id: the same teacher always shows up with the same color. */
export function avatarColor(id: number): string {
  return AVATAR_COLORS[Math.abs(id) % AVATAR_COLORS.length];
}
