import { IcDoc, IcMail, IcPhone } from '@/shared/components/ui/Icons'
import type { LeadDto } from '../types/lead.types'
import { timeAgo } from '../utils/timeAgo'

interface LeadCardProps {
  lead: LeadDto
  dragging: boolean
  onOpen: () => void
  onDragStart: (e: React.DragEvent<HTMLButtonElement>) => void
  onDragEnd: () => void
}

export function LeadCard({ lead, dragging, onOpen, onDragStart, onDragEnd }: LeadCardProps) {
  const propertyLabel = lead.propertyTitle ?? (lead.listingId ? 'Publicación' : 'Consulta general')

  return (
    <button
      type="button"
      className={`lead-card-item${dragging ? ' dragging' : ''}`}
      draggable
      onDragStart={onDragStart}
      onDragEnd={onDragEnd}
      onClick={onOpen}
      aria-label={`Consulta de ${lead.name} — ${propertyLabel}`}
    >
      <div className="lci-top">
        <span className="lci-name">{lead.name}</span>
        {lead.notesCount > 0 && (
          <span className="chip" title={`${lead.notesCount} nota${lead.notesCount === 1 ? '' : 's'}`}>
            <IcDoc size={10} />{lead.notesCount}
          </span>
        )}
      </div>
      <div className="lci-property">{propertyLabel}</div>
      <div className="lci-meta">
        <span className="lci-channel">
          {lead.email && <IcMail size={12} />}
          {lead.phone && <IcPhone size={12} />}
        </span>
        <span>{timeAgo(lead.createdAt)}</span>
      </div>
    </button>
  )
}
