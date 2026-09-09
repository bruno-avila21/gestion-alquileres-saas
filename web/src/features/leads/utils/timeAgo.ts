const MIN = 60_000
const HOUR = 3_600_000
const DAY = 86_400_000
const WEEK = 604_800_000
const MONTH = 30 * DAY

/**
 * "hace cuánto" relativo para la tarjeta del Kanban (createdAt). Más allá de ~45 días deja de tener
 * sentido decir "hace X semanas" y se muestra la fecha corta.
 */
export function timeAgo(iso: string): string {
  const diffMs = Date.now() - new Date(iso).getTime()
  if (diffMs < MIN) return 'recién'
  if (diffMs < HOUR) return `hace ${Math.floor(diffMs / MIN)} min`
  if (diffMs < DAY) return `hace ${Math.floor(diffMs / HOUR)} h`
  if (diffMs < WEEK) return `hace ${Math.floor(diffMs / DAY)} d`
  if (diffMs < 1.5 * MONTH) return `hace ${Math.floor(diffMs / WEEK)} sem`
  return new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: 'short', year: 'numeric' }).format(new Date(iso))
}
