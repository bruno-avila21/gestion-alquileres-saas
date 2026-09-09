import { useState } from 'react'
import { QueryError } from '@/shared/components/ui/QueryError'
import { LEAD_STATUS_LABELS } from '../types/lead.types'
import type { LeadDto, LeadStatus } from '../types/lead.types'
import { LeadCard } from './LeadCard'

interface LeadColumnProps {
  status: LeadStatus
  leads: LeadDto[]
  count: number
  isLoading: boolean
  isError: boolean
  draggingId: string | null
  onRetry: () => void
  onOpen: (lead: LeadDto) => void
  onDragStartCard: (id: string) => void
  onDragEndCard: () => void
  onDropStatus: (id: string, status: LeadStatus) => void
}

export function LeadColumn({
  status, leads, count, isLoading, isError, draggingId, onRetry,
  onOpen, onDragStartCard, onDragEndCard, onDropStatus,
}: LeadColumnProps) {
  const [dragOver, setDragOver] = useState(false)

  function handleDrop(e: React.DragEvent<HTMLDivElement>) {
    e.preventDefault()
    setDragOver(false)
    const id = e.dataTransfer.getData('text/plain')
    if (id) onDropStatus(id, status)
  }

  return (
    <div
      className={`kanban-col${dragOver ? ' drag-over' : ''}`}
      onDragOver={(e) => { e.preventDefault(); setDragOver(true) }}
      onDragLeave={() => setDragOver(false)}
      onDrop={handleDrop}
    >
      <div className="kanban-col-h">
        <span className="nm">{LEAD_STATUS_LABELS[status]}</span>
        <span className="cnt">{count}</span>
      </div>
      <div className="kanban-col-body">
        {isError ? (
          <QueryError onRetry={onRetry} message="No pudimos cargar esta columna." />
        ) : isLoading ? (
          <div className="kanban-col-empty">Cargando…</div>
        ) : leads.length === 0 ? (
          <div className="kanban-col-empty">Sin consultas</div>
        ) : (
          leads.map((lead) => (
            <LeadCard
              key={lead.id}
              lead={lead}
              dragging={draggingId === lead.id}
              onOpen={() => onOpen(lead)}
              onDragStart={(e) => {
                e.dataTransfer.setData('text/plain', lead.id)
                e.dataTransfer.effectAllowed = 'move'
                onDragStartCard(lead.id)
              }}
              onDragEnd={onDragEndCard}
            />
          ))
        )}
      </div>
    </div>
  )
}
