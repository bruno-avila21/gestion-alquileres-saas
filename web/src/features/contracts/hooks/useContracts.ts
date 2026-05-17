import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { contractService } from '../services/contractService'
import type { ContractStatus, CreateContractRequest, TerminateContractRequest, UpdateContractRequest } from '../types/contract.types'

const KEY = ['contracts']

export function useContracts(params?: { tenantId?: string; propertyId?: string; status?: ContractStatus }) {
  return useQuery({ queryKey: [...KEY, params], queryFn: () => contractService.list(params) })
}

export function useCreateContract() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (req: CreateContractRequest) => contractService.create(req),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}

export function useUpdateContract() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, req }: { id: string; req: UpdateContractRequest }) => contractService.update(id, req),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}

export function useTerminateContract() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, req }: { id: string; req: TerminateContractRequest }) => contractService.terminate(id, req),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}
