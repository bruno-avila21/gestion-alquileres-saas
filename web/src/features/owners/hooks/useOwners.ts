import { useMutation, useQuery } from '@tanstack/react-query'
import { ownerService } from '../services/ownerService'
import type { SimulateAdjustmentParams } from '../types/owner.types'

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

/** Todas las liquidaciones del período, para la vista de lote. */
export function useAllSettlements(from: string, to: string) {
  return useQuery({
    queryKey: [...OWNERS_KEY, 'settlements', from, to],
    queryFn: () => ownerService.listSettlements(from, to),
  })
}

/**
 * Simulación de ajuste. Va como mutación y no como query a propósito: es una acción que
 * el usuario dispara con un botón, no un dato que la pantalla tenga que tener cargado.
 */
export function useSimulateAdjustment() {
  return useMutation({
    mutationFn: (params: SimulateAdjustmentParams) => ownerService.simulateAdjustment(params),
  })
}
