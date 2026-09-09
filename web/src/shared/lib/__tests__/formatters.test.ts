import { describe, expect, it } from 'vitest'
import { formatDate, formatDateShort, formatPeriod, parseApiDate } from '../formatters'

describe('parseApiDate', () => {
  // Regresión (auditoría 2026-07-31): `new Date('2026-01-01')` es medianoche UTC, que en
  // Argentina (UTC-3) cae el 31/12/2025. Estas aserciones son independientes de la zona
  // horaria de quien corra los tests: fallan en cualquier huso al oeste de UTC sin el fix.
  it('interpreta una fecha date-only como día del calendario local', () => {
    const d = parseApiDate('2026-01-01')
    expect([d.getFullYear(), d.getMonth(), d.getDate()]).toEqual([2026, 0, 1])
  })

  it('no corre el día en el borde de fin de año', () => {
    const d = parseApiDate('2026-12-31')
    expect([d.getFullYear(), d.getMonth(), d.getDate()]).toEqual([2026, 11, 31])
  })

  it('respeta los años bisiestos', () => {
    const d = parseApiDate('2028-02-29')
    expect([d.getFullYear(), d.getMonth(), d.getDate()]).toEqual([2028, 1, 29])
  })

  // Los DateTimeOffset son instantes reales: convertirlos a hora local sí corresponde,
  // así que ese camino no debe cambiar.
  it('deja intactos los timestamps completos', () => {
    expect(parseApiDate('2026-03-15T12:00:00Z').toISOString()).toBe('2026-03-15T12:00:00.000Z')
  })
})

describe('formatDate', () => {
  it('muestra el mismo día del calendario que vino de la API', () => {
    const formatted = formatDate('2026-01-01')
    expect(formatted).toContain('2026')
    expect(formatted).toMatch(/\b01\b/)
  })

  it('no adelanta el año en el último día', () => {
    expect(formatDate('2026-12-31')).toContain('2026')
  })
})

describe('formatDateShort', () => {
  it('muestra el día correcto', () => {
    expect(formatDateShort('2026-01-01')).toMatch(/\b01\b/)
  })
})

describe('formatPeriod', () => {
  // La API manda el período como DateOnly ("2026-05-01"), pero algunas vistas usan "2026-05".
  it('acepta el período con día', () => {
    expect(formatPeriod('2026-05-01')).toBe('May 2026')
  })

  it('acepta el período sin día', () => {
    expect(formatPeriod('2026-05')).toBe('May 2026')
  })
})
