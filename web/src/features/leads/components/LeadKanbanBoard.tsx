import { useState } from 'react'
import { useLeads, useLeadSummary, useUpdateLeadStatus } from '../hooks/useLeads'
import { LEAD_STATUSES } from '../types/lead.types'
import type { LeadDto, LeadStatus } from '../types/lead.types'
import { LeadColumn } from './LeadColumn'
import { LostReasonModal } from './LostReasonModal'

const BOARD_PAGE_SIZE = 200

interface LeadKanbanBoardProps {
  search: string
  onOpenLead: (lead: LeadDto) => void
}

/**
 * El encabezado de cada columna muestra el conteo de `summary` (los totales reales de la org, no
 * afectados por el buscador); las tarjetas debajo muestran el resultado de `search`. Puede haber
 * más consultas en una columna que tarjetas visibles si hay un texto buscado — es intencional.
 */
export function LeadKanbanBoard({ search, onOpenLead }: LeadKanbanBoardProps) {
  const { data, isLoading, isError, refetch } = useLeads({ search, page: 1, pageSize: BOARD_PAGE_SIZE })
  const { data: summary } = useLeadSummary()
  const updateStatus = useUpdateLeadStatus()

  const [draggingId, setDraggingId] = useState<string | null>(null)
  const [lostTarget, setLostTarget] = useState<LeadDto | null>(null)

  const leadsByStatus: Record<LeadStatus, LeadDto[]> = {
    New: [], Contacted: [], Visit: [], Negotiation: [], Won: [], Lost: [],
  }
  data?.items.forEach((lead) => { leadsByStatus[lead.status].push(lead) })

  function handleDropStatus(id: string, status: LeadStatus) {
    const lead = data?.items.find((l) => l.id === id)
    if (!lead || lead.status === status) return
    if (status === 'Lost') {
      setLostTarget(lead)
      return
    }
    updateStatus.mutate({ id, req: { status } })
  }

  function confirmLost(reason: string) {
    if (!lostTarget) return
    updateStatus.mutate({ id: lostTarget.id, req: { status: 'Lost', lostReason: reason } })
    setLostTarget(null)
  }

  return (
    <>
      <div className="kanban">
        {LEAD_STATUSES.map((status) => (
          <LeadColumn
            key={status}
            status={status}
            leads={leadsByStatus[status]}
            count={summary?.byStatus[status] ?? leadsByStatus[status].length}
            isLoading={isLoading}
            isError={isError}
            draggingId={draggingId}
            onRetry={() => refetch()}
            onOpen={onOpenLead}
            onDragStartCard={setDraggingId}
            onDragEndCard={() => setDraggingId(null)}
            onDropStatus={handleDropStatus}
          />
        ))}
      </div>

      <LostReasonModal
        open={!!lostTarget}
        leadName={lostTarget?.name ?? ''}
        pending={updateStatus.isPending}
        onConfirm={confirmLost}
        onCancel={() => setLostTarget(null)}
      />
    </>
  )
}
