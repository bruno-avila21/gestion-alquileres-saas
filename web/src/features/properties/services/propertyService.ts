import { api } from '@/shared/lib/api'
import type { CreatePropertyRequest, PropertyDto, UpdatePropertyRequest } from '../types/property.types'

export const propertyService = {
  async list(): Promise<PropertyDto[]> {
    const { data } = await api.get<PropertyDto[]>('/properties')
    return data
  },
  async getById(id: string): Promise<PropertyDto> {
    const { data } = await api.get<PropertyDto>(`/properties/${id}`)
    return data
  },
  async create(req: CreatePropertyRequest): Promise<PropertyDto> {
    const { data } = await api.post<PropertyDto>('/properties', req)
    return data
  },
  async update(id: string, req: UpdatePropertyRequest): Promise<PropertyDto> {
    const { data } = await api.put<PropertyDto>(`/properties/${id}`, req)
    return data
  },
  async remove(id: string): Promise<void> {
    await api.delete(`/properties/${id}`)
  },
}
