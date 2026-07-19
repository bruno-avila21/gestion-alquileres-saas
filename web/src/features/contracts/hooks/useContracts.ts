import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { contractService } from '../services/contractService'
import type {
  AdjustmentType,
  ApplyAdjustmentRequest,
  ContractStatus,
  TransactionType,
  CreateContractRequest,
  RegisterManualTransactionRequest,
  RegisterPaymentRequest,
  TerminateContractRequest,
  UpdateContractRequest,
} from '../types/contract.types'

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

export function useContractById(id: string | undefined) {
  return useQuery({
    queryKey: [...KEY, id],
    queryFn: () => contractService.getById(id!),
    enabled: !!id,
  })
}

export function useRentHistory(contractId: string | undefined) {
  return useQuery({
    queryKey: ['contracts', contractId, 'history'],
    queryFn: () => contractService.getHistory(contractId!),
    enabled: !!contractId,
  })
}

export function useAdjustmentProjection(contractId: string | undefined) {
  return useQuery({
    queryKey: ['contracts', contractId, 'adjustment-projection'],
    queryFn: () => contractService.getAdjustmentProjection(contractId!),
    enabled: !!contractId,
  })
}

export function useTransactions(contractId: string | undefined) {
  return useQuery({
    queryKey: ['contracts', contractId, 'transactions'],
    queryFn: () => contractService.getTransactions(contractId!),
    enabled: !!contractId,
  })
}

export function useApplyAdjustment() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ contractId, req }: { contractId: string; req: ApplyAdjustmentRequest }) =>
      contractService.applyAdjustment(contractId, req),
    onSuccess: (_data, { contractId }) => {
      qc.invalidateQueries({ queryKey: [...KEY, contractId] })
      qc.invalidateQueries({ queryKey: ['contracts', contractId, 'history'] })
    },
  })
}

export function useRegisterPayment() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ contractId, req }: { contractId: string; req: RegisterPaymentRequest }) =>
      contractService.registerPayment(contractId, req),
    onSuccess: (_data, { contractId }) => {
      qc.invalidateQueries({ queryKey: ['contracts', contractId, 'transactions'] })
    },
  })
}

export function useRegisterManualCharge() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ contractId, req }: { contractId: string; req: RegisterManualTransactionRequest }) =>
      contractService.registerManualCharge(contractId, req),
    onSuccess: (_data, { contractId }) => {
      qc.invalidateQueries({ queryKey: ['contracts', contractId, 'transactions'] })
    },
  })
}

export function useAllTransactions(params: { page: number; pageSize: number; type?: TransactionType; search?: string }) {
  return useQuery({
    queryKey: ['transactions', 'all', params],
    queryFn: () => contractService.listAllTransactions(params),
    placeholderData: keepPreviousData,
  })
}

export function useAllRentHistory(params: { page: number; pageSize: number; type?: AdjustmentType; search?: string }) {
  return useQuery({
    queryKey: ['rent-adjustments', 'all', params],
    queryFn: () => contractService.listAllRentHistory(params),
    placeholderData: keepPreviousData,
  })
}
