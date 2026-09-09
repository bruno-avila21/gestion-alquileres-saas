import { IcMail, IcPhone } from '@/shared/components/ui/Icons'
import { LEAD_STATUS_LABELS, type LeadDto, type LeadStatus } from '../types/lead.types'
import { timeAgo } from '../utils/timeAgo'

/** Cada estado con el chip que le corresponde, para que el color signifique lo mismo que en el tablero. */
const STATUS_CHIP: Record<LeadStatus, string> = {
  New: 'chip--info',
  Contacted: '',
  Visit: 'chip--icl',
  Negotiation: 'chip--warn',
  Won: 'chip--ok',
  Lost: 'chip--danger',
}

interface Props {
  leads: LeadDto[]
  isLoading: boolean
  onOpen: (lead: LeadDto) => void
}

/**
 * Vista de lista del CRM: la misma cartera del tablero, ordenada por actividad.
 * El Kanban sirve para mover un lead de etapa; esta vista sirve para la pregunta
 * que el tablero contesta mal — "¿a quién no le contesté hace más tiempo?".
 */
export function LeadListTable({ leads, isLoading, onOpen }: Props) {
  if (isLoading) {
    return <div className="card" style={{ padding: 32, textAlign: 'center', color: 'var(--muted)' }}>Cargando…</div>
  }

  if (leads.length === 0) {
    return (
      <div className="card" style={{ padding: 40, textAlign: 'center', color: 'var(--muted)' }}>
        No hay consultas con esos filtros.
      </div>
    )
  }

  const ordenadas = [...leads].sort((a, b) =>
    (b.lastContactAt ?? b.createdAt).localeCompare(a.lastContactAt ?? a.createdAt))

  return (
    <div className="card">
      <table className="tbl">
        <thead>
          <tr>
            <th>Interesado</th>
            <th>Propiedad</th>
            <th>Origen</th>
            <th>Estado</th>
            <th>Última actividad</th>
          </tr>
        </thead>
        <tbody>
          {ordenadas.map((lead) => (
            <tr
              key={lead.id}
              style={{ cursor: 'pointer' }}
              onClick={() => onOpen(lead)}
            >
              <td>
                <div style={{ fontWeight: 500 }}>{lead.name}</div>
                <div className="row" style={{ gap: 10, fontSize: 'var(--fs-xs)', color: 'var(--muted)', marginTop: 2 }}>
                  {lead.email ? <span className="row" style={{ gap: 4 }}><IcMail size={11} />{lead.email}</span> : null}
                  {lead.phone ? <span className="row" style={{ gap: 4 }}><IcPhone size={11} />{lead.phone}</span> : null}
                  {!lead.email && !lead.phone ? <span>Sin datos de contacto</span> : null}
                </div>
              </td>
              <td className="muted">{lead.propertyTitle ?? 'Consulta general'}</td>
              <td>
                <span className="chip">
                  {lead.source === 'Website' ? 'Sitio' : 'Carga manual'}
                </span>
              </td>
              <td>
                <span className={`chip ${STATUS_CHIP[lead.status]}`}>
                  <span className="dot" />{LEAD_STATUS_LABELS[lead.status]}
                </span>
              </td>
              <td className="muted" style={{ fontSize: 'var(--fs-xs)', whiteSpace: 'nowrap' }}>
                {timeAgo(lead.lastContactAt ?? lead.createdAt)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
