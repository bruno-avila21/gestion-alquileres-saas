import { api } from '@/shared/lib/api'
import type { AppTenantDto, CreateAppTenantRequest, InviteTenantResult, UpdateAppTenantRequest } from '../types/apptenant.types'

export const appTenantService = {
  async list(): Promise<AppTenantDto[]> {
    const { data } = await api.get<AppTenantDto[]>('/tenants')
    return data
  },
  async getById(id: string): Promise<AppTenantDto> {
    const { data } = await api.get<AppTenantDto>(`/tenants/${id}`)
    return data
  },
  async create(req: CreateAppTenantRequest): Promise<AppTenantDto> {
    const { data } = await api.post<AppTenantDto>('/tenants', req)
    return data
  },
  async update(id: string, req: UpdateAppTenantRequest): Promise<AppTenantDto> {
    const { data } = await api.put<AppTenantDto>(`/tenants/${id}`, req)
    return data
  },
  async remove(id: string): Promise<void> {
    await api.delete(`/tenants/${id}`)
  },
  async invite(id: string): Promise<InviteTenantResult> {
    const { data } = await api.post<InviteTenantResult>(`/tenants/${id}/invite`)
    return data
  },
}
