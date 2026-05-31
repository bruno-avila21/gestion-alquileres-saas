import { api } from '@/shared/lib/api'
import type {
  ApplyAdjustmentRequest,
  ContractDto,
  ContractStatus,
  CreateContractRequest,
  RegisterManualTransactionRequest,
  RegisterPaymentRequest,
  RentHistoryDto,
  TerminateContractRequest,
  TransactionDto,
  UpdateContractRequest,
} from '../types/contract.types'

export const contractService = {
  async list(params?: { tenantId?: string; propertyId?: string; status?: ContractStatus }): Promise<ContractDto[]> {
    const { data } = await api.get<ContractDto[]>('/contracts', { params })
    return data
  },
  async getById(id: string): Promise<ContractDto> {
    const { data } = await api.get<ContractDto>(`/contracts/${id}`)
    return data
  },
  async create(req: CreateContractRequest): Promise<ContractDto> {
    const { data } = await api.post<ContractDto>('/contracts', req)
    return data
  },
  async update(id: string, req: UpdateContractRequest): Promise<ContractDto> {
    const { data } = await api.put<ContractDto>(`/contracts/${id}`, req)
    return data
  },
  async terminate(id: string, req: TerminateContractRequest): Promise<ContractDto> {
    const { data } = await api.post<ContractDto>(`/contracts/${id}/terminate`, req)
    return data
  },
  async getHistory(contractId: string): Promise<RentHistoryDto[]> {
    const { data } = await api.get<RentHistoryDto[]>(`/contracts/${contractId}/history`)
    return data
  },
  async getTransactions(contractId: string): Promise<TransactionDto[]> {
    const { data } = await api.get<TransactionDto[]>(`/contracts/${contractId}/transactions`)
    return data
  },
  async applyAdjustment(contractId: string, req: ApplyAdjustmentRequest): Promise<RentHistoryDto> {
    const { data } = await api.post<RentHistoryDto>(`/contracts/${contractId}/adjust`, req)
    return data
  },
  async registerPayment(contractId: string, req: RegisterPaymentRequest): Promise<TransactionDto> {
    const { data } = await api.post<TransactionDto>(`/contracts/${contractId}/payments`, req)
    return data
  },
  async registerManualCharge(contractId: string, req: RegisterManualTransactionRequest): Promise<TransactionDto> {
    const { data } = await api.post<TransactionDto>(`/contracts/${contractId}/charges`, req)
    return data
  },
  async listAllTransactions(): Promise<TransactionDto[]> {
    const { data } = await api.get<TransactionDto[]>('/transactions')
    return data
  },
  async listAllRentHistory(): Promise<RentHistoryDto[]> {
    const { data } = await api.get<RentHistoryDto[]>('/rent-adjustments')
    return data
  },
  async exportTransactionsCsv(): Promise<Blob> {
    const { data } = await api.get<Blob>('/transactions/export', { responseType: 'blob' })
    return data
  },
  async exportAdjustmentsCsv(): Promise<Blob> {
    const { data } = await api.get<Blob>('/rent-adjustments/export', { responseType: 'blob' })
    return data
  },
}
