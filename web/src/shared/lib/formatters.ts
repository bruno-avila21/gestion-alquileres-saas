export const formatARS = (amount: number) =>
  new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 }).format(amount)

export const formatPct = (n: number) => `${n.toFixed(2)}%`

export const formatDate = (iso: string) =>
  new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: 'short', year: 'numeric' }).format(new Date(iso))

export const formatDateShort = (iso: string) =>
  new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: 'short' }).format(new Date(iso))

export const formatPeriod = (period: string) => {
  const [year, month] = period.split('-')
  const months = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic']
  return `${months[parseInt(month) - 1]} ${year}`
}
