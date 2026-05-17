import { api } from '@/shared/lib/api'
import type {
  ContractDto,
  ContractStatus,
  CreateContractRequest,
  TerminateContractRequest,
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
}
