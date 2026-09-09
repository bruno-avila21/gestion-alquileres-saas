export type PropertyType = 'House' | 'Apartment' | 'PH' | 'Commercial' | 'Office' | 'Land' | 'Other'

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
  neighborhood: string | null
  code: string | null
  description: string | null
  rooms: number | null
  bedrooms: number | null
  bathrooms: number | null
  garages: number | null
  ageYears: number | null
  coveredAreaM2: number | null
  latitude: number | null
  longitude: number | null
  suitableForCredit: boolean | null
  features: string[]
}

/** Datos de la "ficha pública" — todos opcionales/nullable, se mandan juntos en `details`. */
export interface PropertyDetailsInput {
  neighborhood?: string | null
  code?: string | null
  description?: string | null
  rooms?: number | null
  bedrooms?: number | null
  bathrooms?: number | null
  garages?: number | null
  ageYears?: number | null
  coveredAreaM2?: number | null
  latitude?: number | null
  longitude?: number | null
  suitableForCredit?: boolean | null
  features?: string[]
}

export interface CreatePropertyRequest {
  address: string
  city: string
  province: string
  propertyType: PropertyType
  areaM2?: number | null
  notes?: string | null
  details?: PropertyDetailsInput
}

export interface UpdatePropertyRequest extends CreatePropertyRequest {
  isActive: boolean
}

export interface PropertyPhotoDto {
  id: string
  propertyId: string
  url: string
  mimeType: string
  sizeBytes: number
  sortOrder: number
  isCover: boolean
  createdAt: string
}
