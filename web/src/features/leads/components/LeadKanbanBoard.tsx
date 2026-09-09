import { useEffect, useRef, useState } from 'react'
import { useLeads, useLeadSummary, useUpdateLeadStatus } from '../hooks/useLeads'
import { LEAD_STATUSES } from '../types/lead.types'
import type { LeadDto, LeadStatus } from '../types/lead.types'
import { LeadColumn } from './LeadColumn'
import { LostReasonModal } from './LostReasonModal'

const BOARD_PAGE_SIZE = 200

/** Franja desde cada borde del tablero, en px, donde arrastrar una tarjeta dispara autoscroll. */
const AUTOSCROLL_EDGE_PX = 60
/** Velocidad máxima (px por frame) al borde mismo de la franja; decrece a 0 en su límite exterior. */
const AUTOSCROLL_MAX_SPEED = 18
/** Con `prefers-reduced-motion`, en vez de animar cada frame se salta de a este tamaño... */
const AUTOSCROLL_REDUCED_STEP_PX = 90
/** ...como máximo una vez por este intervalo. */
const AUTOSCROLL_REDUCED_INTERVAL_MS = 300

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

  const boardRef = useRef<HTMLDivElement>(null)
  const scrollVelocityRef = useRef(0)
  const scrollFrameRef = useRef<number | null>(null)
  const lastReducedStepAtRef = useRef(0)
  const reducedMotionRef = useRef(false)

  useEffect(() => {
    const mql = window.matchMedia('(prefers-reduced-motion: reduce)')
    reducedMotionRef.current = mql.matches
    const onChange = (e: MediaQueryListEvent) => { reducedMotionRef.current = e.matches }
    mql.addEventListener('change', onChange)
    return () => mql.removeEventListener('change', onChange)
  }, [])

  function stopAutoScroll() {
    scrollVelocityRef.current = 0
    if (scrollFrameRef.current !== null) {
      cancelAnimationFrame(scrollFrameRef.current)
      scrollFrameRef.current = null
    }
  }

  // Cancelar cualquier autoscroll en curso si el componente se desmonta a mitad de un drag.
  useEffect(() => stopAutoScroll, [])

  function stepAutoScroll() {
    const el = boardRef.current
    if (!el || scrollVelocityRef.current === 0) {
      scrollFrameRef.current = null
      return
    }
    el.scrollLeft += scrollVelocityRef.current
    scrollFrameRef.current = requestAnimationFrame(stepAutoScroll)
  }

  /** Proximidad 0→1 al borde más cercano dentro de la franja de autoscroll; 0 fuera de ella. */
  function edgeProximity(clientX: number, rect: DOMRect): number {
    const fromLeft = clientX - rect.left
    const fromRight = rect.right - clientX
    if (fromLeft < AUTOSCROLL_EDGE_PX) return -(AUTOSCROLL_EDGE_PX - Math.max(0, fromLeft)) / AUTOSCROLL_EDGE_PX
    if (fromRight < AUTOSCROLL_EDGE_PX) return (AUTOSCROLL_EDGE_PX - Math.max(0, fromRight)) / AUTOSCROLL_EDGE_PX
    return 0
  }

  function handleBoardDragOver(e: React.DragEvent<HTMLDivElement>) {
    if (!draggingId) return
    const el = boardRef.current
    if (!el) return
    const proximity = edgeProximity(e.clientX, el.getBoundingClientRect())

    if (reducedMotionRef.current) {
      stopAutoScroll()
      if (proximity === 0) return
      const now = performance.now()
      if (now - lastReducedStepAtRef.current < AUTOSCROLL_REDUCED_INTERVAL_MS) return
      lastReducedStepAtRef.current = now
      el.scrollBy({ left: proximity > 0 ? AUTOSCROLL_REDUCED_STEP_PX : -AUTOSCROLL_REDUCED_STEP_PX })
      return
    }

    scrollVelocityRef.current = proximity * AUTOSCROLL_MAX_SPEED
    if (scrollVelocityRef.current !== 0 && scrollFrameRef.current === null) {
      scrollFrameRef.current = requestAnimationFrame(stepAutoScroll)
    }
  }

  function handleBoardDragLeave(e: React.DragEvent<HTMLDivElement>) {
    if (!e.currentTarget.contains(e.relatedTarget as Node | null)) stopAutoScroll()
  }

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
      <div
        ref={boardRef}
        className="kanban"
        onDragOver={handleBoardDragOver}
        onDragLeave={handleBoardDragLeave}
        onDrop={stopAutoScroll}
      >
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
            onDragEndCard={() => { setDraggingId(null); stopAutoScroll() }}
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
