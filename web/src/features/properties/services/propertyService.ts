import { api } from '@/shared/lib/api'
import type {
  CreatePropertyRequest, PropertyDto, PropertyPhotoDto, UpdatePropertyRequest,
} from '../types/property.types'

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

  async listPhotos(propertyId: string): Promise<PropertyPhotoDto[]> {
    const { data } = await api.get<PropertyPhotoDto[]>(`/properties/${propertyId}/photos`)
    return data
  },
  async uploadPhoto(propertyId: string, file: File): Promise<PropertyPhotoDto> {
    const form = new FormData()
    form.append('file', file)
    const { data } = await api.post<PropertyPhotoDto>(`/properties/${propertyId}/photos`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    return data
  },
  async setCoverPhoto(propertyId: string, photoId: string): Promise<PropertyPhotoDto> {
    const { data } = await api.put<PropertyPhotoDto>(`/properties/${propertyId}/photos/${photoId}/cover`)
    return data
  },
  async deletePhoto(propertyId: string, photoId: string): Promise<void> {
    await api.delete(`/properties/${propertyId}/photos/${photoId}`)
  },
}
