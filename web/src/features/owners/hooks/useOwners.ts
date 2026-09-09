import { useQuery } from '@tanstack/react-query'
import { ownerService } from '../services/ownerService'

export const OWNERS_KEY = ['owners'] as const

export function useOwners() {
  return useQuery({ queryKey: OWNERS_KEY, queryFn: ownerService.list })
}

/** Rendición de un propietario en el período [from, to] (fechas ISO `yyyy-MM-dd`). */
export function useOwnerSettlement(ownerId: string | null, from: string, to: string, enabled: boolean) {
  return useQuery({
    queryKey: [...OWNERS_KEY, ownerId, 'settlement', from, to],
    queryFn: () => ownerService.getSettlement(ownerId as string, from, to),
    enabled: enabled && !!ownerId,
  })
}
