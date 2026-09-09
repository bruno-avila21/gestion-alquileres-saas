const DATE_ONLY = /^(\d{4})-(\d{2})-(\d{2})$/

/**
 * La API devuelve dos formas de fecha distintas:
 *   - `DateOnly`       → `"2026-01-01"`             (sin hora ni zona: un día del calendario)
 *   - `DateTimeOffset` → `"2026-01-01T14:03:22Z"`   (un instante real en el tiempo)
 *
 * `new Date("2026-01-01")` interpreta el string date-only como **medianoche UTC**, que en
 * Argentina (UTC-3) es el día ANTERIOR. Por eso las fechas de inicio y fin de contrato,
 * vencimientos y fechas efectivas de ajuste se mostraban corridas un día.
 *
 * Los instantes completos sí se parsean con `new Date`: ahí convertir a hora local es
 * exactamente lo que corresponde.
 */
export const parseApiDate = (iso: string): Date => {
  const m = DATE_ONLY.exec(iso)
  return m ? new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3])) : new Date(iso)
}

export const formatARS = (amount: number) =>
  new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 }).format(amount)

/** Formatea un importe en la moneda indicada (publicaciones pueden ser ARS o USD). */
export const formatMoney = (amount: number, currency: 'ARS' | 'USD') =>
  new Intl.NumberFormat('es-AR', { style: 'currency', currency, maximumFractionDigits: 0 }).format(amount)

/**
 * Importe con centavos. `formatARS` redondea a pesos enteros, que es lo que se quiere para
 * alquileres y totales de cartera, pero NO para la rendición al propietario: la comisión es un
 * porcentaje del cobrado y casi nunca cae en peso redondo, y el PDF que firma la inmobiliaria
 * muestra los centavos. Redondear en pantalla lo que se le paga a una persona induce a error.
 */
export const formatARSExact = (amount: number) =>
  new Intl.NumberFormat('es-AR', {
    style: 'currency',
    currency: 'ARS',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount)

export const formatPct = (n: number) => `${n.toFixed(2)}%`

export const formatDate = (iso: string) =>
  new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: 'short', year: 'numeric' }).format(parseApiDate(iso))

export const formatDateShort = (iso: string) =>
  new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: 'short' }).format(parseApiDate(iso))

/** Período mensual: acepta tanto `"2026-05"` como el `"2026-05-01"` que manda la API. */
export const formatPeriod = (period: string) => {
  const [year, month] = period.split('-')
  const months = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic']
  return `${months[parseInt(month) - 1]} ${year}`
}
