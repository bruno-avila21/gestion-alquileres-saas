import { api } from '@/shared/lib/api'
import type {
  AddLeadNoteRequest, CreateLeadRequest, LeadDetailDto, LeadDto, LeadFilters, LeadNoteDto,
  LeadSummaryDto, PagedResult, UpdateLeadRequest, UpdateLeadStatusRequest,
} from '../types/lead.types'

function buildFilterParams(filters: LeadFilters): Record<string, string | number> {
  const params: Record<string, string | number> = {}
  if (filters.status) params.status = filters.status
  if (filters.search?.trim()) params.search = filters.search.trim()
  params.page = filters.page ?? 1
  params.pageSize = filters.pageSize ?? 50
  return params
}

export const leadService = {
  async list(filters: LeadFilters): Promise<PagedResult<LeadDto>> {
    const { data } = await api.get<PagedResult<LeadDto>>('/leads', { params: buildFilterParams(filters) })
    return data
  },
  async summary(): Promise<LeadSummaryDto> {
    const { data } = await api.get<LeadSummaryDto>('/leads/summary')
    return data
  },
  async getById(id: string): Promise<LeadDetailDto> {
    const { data } = await api.get<LeadDetailDto>(`/leads/${id}`)
    return data
  },
  async create(req: CreateLeadRequest): Promise<LeadDto> {
    const { data } = await api.post<LeadDto>('/leads', req)
    return data
  },
  async update(id: string, req: UpdateLeadRequest): Promise<LeadDto> {
    const { data } = await api.put<LeadDto>(`/leads/${id}`, req)
    return data
  },
  async updateStatus(id: string, req: UpdateLeadStatusRequest): Promise<LeadDto> {
    const { data } = await api.patch<LeadDto>(`/leads/${id}/status`, req)
    return data
  },
  async addNote(id: string, req: AddLeadNoteRequest): Promise<LeadNoteDto> {
    const { data } = await api.post<LeadNoteDto>(`/leads/${id}/notes`, req)
    return data
  },
  async remove(id: string): Promise<void> {
    await api.delete(`/leads/${id}`)
  },
}
