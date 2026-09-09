export type PublicOperationType = 'Sale' | 'Rent' | 'TemporaryRent'

export type PublicPropertyType = 'House' | 'Apartment' | 'Commercial' | 'Land' | 'Other' | 'PH' | 'Office'

export type PublicCurrency = 'ARS' | 'USD'

export type PublicSortOption = 'price_asc' | 'price_desc' | 'rooms_asc' | 'rooms_desc' | 'newest'

export interface PublicOrgDto {
  name: string
  slug: string
}

export interface FacetDto {
  value: string
  count: number
}

export interface PublicListingFacets {
  operationTypes: FacetDto[]
  propertyTypes: FacetDto[]
  cities: FacetDto[]
  neighborhoods: FacetDto[]
  currencies: FacetDto[]
  rooms: FacetDto[]
  bedrooms: FacetDto[]
  features: FacetDto[]
  suitableForCredit: FacetDto[]
}

export interface PublicListingCard {
  id: string
  operationType: PublicOperationType
  price: number
  currency: PublicCurrency
  expenses: number | null
  title: string
  isFeatured: boolean
  propertyType: PublicPropertyType
  address: string
  neighborhood: string | null
  city: string
  code: string | null
  rooms: number | null
  bedrooms: number | null
  bathrooms: number | null
  garages: number | null
  areaM2: number | null
  coveredAreaM2: number | null
  coverPhotoUrl: string | null
  publishedAt: string | null
}

export interface PublicListingDetail {
  id: string
  operationType: PublicOperationType
  price: number
  currency: PublicCurrency
  expenses: number | null
  title: string
  isFeatured: boolean
  propertyType: PublicPropertyType
  address: string
  neighborhood: string | null
  city: string
  province: string
  code: string | null
  description: string | null
  rooms: number | null
  bedrooms: number | null
  bathrooms: number | null
  garages: number | null
  ageYears: number | null
  areaM2: number | null
  coveredAreaM2: number | null
  latitude: number | null
  longitude: number | null
  suitableForCredit: boolean | null
  features: string[]
  photoUrls: string[]
  publishedAt: string | null
}

export interface PublicListingsResponse {
  items: PublicListingCard[]
  total: number
  page: number
  pageSize: number
  facets: PublicListingFacets
}

/**
 * Alta de una consulta (lead) desde el sitio público — `POST /public/{slug}/leads`.
 * `website` es el campo trampa (honeypot): un visitante humano nunca lo completa porque está
 * oculto con CSS; un bot que rellena todos los inputs del formulario, sí. Si llega con contenido
 * la API responde 204 sin crear nada, sin revelar que fue detectado.
 */
export interface CreatePublicLeadRequest {
  name: string
  email?: string
  phone?: string
  message: string
  listingId?: string
  website?: string
}

/** Filtros de búsqueda del listado público. Todo opcional: se omite lo vacío al armar la query. */
export interface PublicListingFilters {
  operation?: PublicOperationType
  type?: PublicPropertyType
  city?: string
  neighborhood?: string
  currency?: PublicCurrency
  minPrice?: number
  maxPrice?: number
  minRooms?: number
  minBedrooms?: number
  minArea?: number
  maxArea?: number
  features?: string[]
  credit?: boolean
  sort?: PublicSortOption
  page?: number
  pageSize?: number
}
