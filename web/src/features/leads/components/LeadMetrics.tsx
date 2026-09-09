import { IcMail, IcCalendar, IcCash, IcTrend } from '@/shared/components/ui/Icons'
import { useLeadSummary } from '../hooks/useLeads'

/**
 * Cinta de indicadores del CRM.
 *
 * Las cuatro cifras salen enteras de `GET /leads/summary` (total + conteo por estado):
 * ninguna es una serie de ejemplo ni un número de maqueta. Si el modelo de Stitch pide
 * un indicador que no tiene dato detrás — "leads nuevos esta semana", "visitas de hoy" —
 * no está acá: un KPI inventado en la pantalla que abre el comercial es exactamente lo
 * que hace dudar del resto del tablero.
 */
export function LeadMetrics() {
  const { data, isLoading } = useLeadSummary()

  const by = data?.byStatus ?? {}
  const won = by.Won ?? 0
  const lost = by.Lost ?? 0
  const cerrados = won + lost
  const activos = Math.max(0, (data?.total ?? 0) - cerrados)

  const cards = [
    {
      lbl: 'Leads activos',
      val: activos,
      hint: 'Sin cerrar todavía',
      icon: <IcMail size={18} />,
      color: 'var(--brand)',
    },
    {
      lbl: 'Visitas coordinadas',
      val: by.Visit ?? 0,
      hint: 'Con visita acordada',
      icon: <IcCalendar size={18} />,
      color: 'var(--icl)',
    },
    {
      lbl: 'En negociación',
      val: by.Negotiation ?? 0,
      hint: 'Alta intención',
      icon: <IcCash size={18} />,
      color: 'var(--warn)',
    },
    {
      lbl: 'Tasa de conversión',
      val: cerrados === 0 ? '—' : `${Math.round((won / cerrados) * 100)}%`,
      hint: cerrados === 0
        ? 'Sin operaciones cerradas aún'
        : `${won} ganada${won === 1 ? '' : 's'} de ${cerrados} cerrada${cerrados === 1 ? '' : 's'}`,
      icon: <IcTrend size={18} />,
      color: 'var(--ok)',
    },
  ]

  return (
    <div className="grid-4">
      {cards.map((c) => (
        <div key={c.lbl} className="card stat">
          <div className="between">
            <span className="lbl">{c.lbl}</span>
            <span style={{ color: c.color, opacity: 0.8 }}>{c.icon}</span>
          </div>
          <div className="val">{isLoading ? '…' : c.val}</div>
          <div style={{ marginTop: 8, fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>
            {isLoading ? '…' : c.hint}
          </div>
        </div>
      ))}
    </div>
  )
}
