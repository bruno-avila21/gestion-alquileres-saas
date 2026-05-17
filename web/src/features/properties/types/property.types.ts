export type PropertyType = 'House' | 'Apartment' | 'Commercial' | 'Land' | 'Other'

export interface PropertyDto {
  id: string
  organizationId: string
  address: string
  city: string
  province: string
  propertyType: PropertyType
  areaM2: number | null
  notes: string | null
  isActive: boolean
  createdAt: string
}

export interface CreatePropertyRequest {
  address: string
  city: string
  province: string
  propertyType: PropertyType
  areaM2?: number | null
  notes?: string | null
}

export interface UpdatePropertyRequest extends CreatePropertyRequest {
  isActive: boolean
}
