import { api } from '@/shared/lib/api'
import type { CreateListingRequest, ListingDto, UpdateListingRequest } from '../types/listing.types'

export const listingService = {
  async listByProperty(propertyId: string): Promise<ListingDto[]> {
    const { data } = await api.get<ListingDto[]>('/listings', { params: { propertyId } })
    return data
  },
  async getById(id: string): Promise<ListingDto> {
    const { data } = await api.get<ListingDto>(`/listings/${id}`)
    return data
  },
  async create(req: CreateListingRequest): Promise<ListingDto> {
    const { data } = await api.post<ListingDto>('/listings', req)
    return data
  },
  async update(id: string, req: UpdateListingRequest): Promise<ListingDto> {
    const { data } = await api.put<ListingDto>(`/listings/${id}`, req)
    return data
  },
  async remove(id: string): Promise<void> {
    await api.delete(`/listings/${id}`)
  },
}
