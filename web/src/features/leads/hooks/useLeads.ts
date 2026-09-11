import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { leadService } from '../services/leadService'
import type {
  AddLeadNoteRequest, CreateLeadRequest, LeadDetailDto, LeadDto, LeadFilters, LeadStatus,
  LeadSummaryDto, PagedResult, UpdateLeadRequest, UpdateLeadStatusRequest,
} from '../types/lead.types'

/**
 * Tres raíces separadas (no un único `['leads']`) a propósito: el update optimista de
 * `useUpdateLeadStatus` pisa las cachés de lista y de summary con `setQueriesData`, que hace match
 * por prefijo. Si todo colgara de `['leads']`, ese `setQueriesData` también encontraría (y
 * corrompería) la caché de detalle, que tiene una forma distinta (`LeadDetailDto`, no paginado).
 */
export const LEADS_LIST_KEY = ['leads', 'list'] as const
export const LEADS_SUMMARY_KEY = ['leads', 'summary'] as const
export const LEAD_DETAIL_KEY = ['leads', 'detail'] as const

export function useLeads(filters: LeadFilters) {
  return useQuery({
    queryKey: [...LEADS_LIST_KEY, filters],
    queryFn: () => leadService.list(filters),
    placeholderData: keepPreviousData,
  })
}

export function useLeadSummary() {
  return useQuery({
    queryKey: LEADS_SUMMARY_KEY,
    queryFn: leadService.summary,
  })
}

export function useLead(id: string | undefined) {
  return useQuery({
    queryKey: [...LEAD_DETAIL_KEY, id],
    queryFn: () => leadService.getById(id!),
    enabled: !!id,
  })
}

export function useCreateLead() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (req: CreateLeadRequest) => leadService.create(req),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: LEADS_LIST_KEY })
      qc.invalidateQueries({ queryKey: LEADS_SUMMARY_KEY })
    },
  })
}

export function useUpdateLead() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, req }: { id: string; req: UpdateLeadRequest }) => leadService.update(id, req),
    onSuccess: (updated) => {
      qc.invalidateQueries({ queryKey: LEADS_LIST_KEY })
      qc.setQueryData<LeadDetailDto>([...LEAD_DETAIL_KEY, updated.id], (prev) =>
        prev ? { ...prev, ...updated } : prev)
    },
  })
}

export function useDeleteLead() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => leadService.remove(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: LEADS_LIST_KEY })
      qc.invalidateQueries({ queryKey: LEADS_SUMMARY_KEY })
    },
  })
}

export function useAddLeadNote() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, req }: { id: string; req: AddLeadNoteRequest }) => leadService.addNote(id, req),
    onSuccess: (_note, { id }) => {
      qc.invalidateQueries({ queryKey: [...LEAD_DETAIL_KEY, id] })
      // La nota suma a notesCount y toca lastContactAt en la tarjeta del tablero.
      qc.invalidateQueries({ queryKey: LEADS_LIST_KEY })
    },
  })
}

interface StatusMutationVars {
  id: string
  req: UpdateLeadStatusRequest
}

interface StatusMutationContext {
  previousLists: [readonly unknown[], PagedResult<LeadDto> | undefined][]
  previousSummary: LeadSummaryDto | undefined
  previousDetail: LeadDetailDto | undefined
}

/**
 * Mueve una consulta de columna en el Kanban. Actualiza la UI al toque (arrastrar y soltar tiene
 * que sentirse instantáneo) y, si el PATCH falla, restaura exactamente lo que había antes en
 * lista, summary y detalle — sin esperar un refetch para enterarse de que no funcionó.
 */
export function useUpdateLeadStatus() {
  const qc = useQueryClient()
  return useMutation<LeadDto, unknown, StatusMutationVars, StatusMutationContext>({
    mutationFn: ({ id, req }: StatusMutationVars) => leadService.updateStatus(id, req),
    onMutate: async ({ id, req }) => {
      await qc.cancelQueries({ queryKey: LEADS_LIST_KEY })
      await qc.cancelQueries({ queryKey: LEADS_SUMMARY_KEY })
      await qc.cancelQueries({ queryKey: [...LEAD_DETAIL_KEY, id] })

      const previousLists = qc.getQueriesData<PagedResult<LeadDto>>({ queryKey: LEADS_LIST_KEY })
      const previousSummary = qc.getQueryData<LeadSummaryDto>(LEADS_SUMMARY_KEY)
      const previousDetail = qc.getQueryData<LeadDetailDto>([...LEAD_DETAIL_KEY, id])

      let fromStatus: LeadStatus | undefined
      const newLostReason = req.status === 'Lost' ? (req.lostReason ?? null) : null

      previousLists.forEach(([key, data]) => {
        if (!data) return
        qc.setQueryData<PagedResult<LeadDto>>(key, {
          ...data,
          items: data.items.map((lead) => {
            if (lead.id !== id) return lead
            fromStatus ??= lead.status
            return { ...lead, status: req.status, lostReason: newLostReason }
          }),
        })
      })

      if (previousSummary && fromStatus && fromStatus !== req.status) {
        qc.setQueryData<LeadSummaryDto>(LEADS_SUMMARY_KEY, {
          ...previousSummary,
          byStatus: {
            ...previousSummary.byStatus,
            [fromStatus]: Math.max(0, (previousSummary.byStatus[fromStatus] ?? 0) - 1),
            [req.status]: (previousSummary.byStatus[req.status] ?? 0) + 1,
          },
        })
      }

      if (previousDetail) {
        qc.setQueryData<LeadDetailDto>([...LEAD_DETAIL_KEY, id], {
          ...previousDetail,
          status: req.status,
          lostReason: newLostReason,
        })
      }

      return { previousLists, previousSummary, previousDetail }
    },
    onError: (_err, { id }, ctx) => {
      if (!ctx) return
      ctx.previousLists.forEach(([key, data]) => qc.setQueryData(key, data))
      if (ctx.previousSummary) qc.setQueryData(LEADS_SUMMARY_KEY, ctx.previousSummary)
      if (ctx.previousDetail) qc.setQueryData([...LEAD_DETAIL_KEY, id], ctx.previousDetail)
    },
    onSettled: (_data, _err, { id }) => {
      qc.invalidateQueries({ queryKey: LEADS_LIST_KEY })
      qc.invalidateQueries({ queryKey: LEADS_SUMMARY_KEY })
      qc.invalidateQueries({ queryKey: [...LEAD_DETAIL_KEY, id] })
    },
  })
}
