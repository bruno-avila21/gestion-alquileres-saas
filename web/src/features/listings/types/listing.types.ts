export type ListingOperationType = 'Sale' | 'Rent' | 'TemporaryRent'
export type ListingCurrency = 'ARS' | 'USD'
export type ListingStatus = 'Draft' | 'Published' | 'Reserved' | 'Sold' | 'Rented' | 'Paused'

export interface ListingDto {
  id: string
  propertyId: string
  operationType: ListingOperationType
  price: number
  currency: ListingCurrency
  expenses: number | null
  status: ListingStatus
  title: string
  isFeatured: boolean
  publishedAt: string | null
  createdAt: string
  updatedAt: string
  propertyAddress: string
  propertyCity: string
  propertyNeighborhood: string | null
  propertyType: string
  propertyCode: string | null
}

export interface CreateListingRequest {
  propertyId: string
  operationType: ListingOperationType
  price: number
  currency: ListingCurrency
  expenses?: number | null
  title: string
  isFeatured?: boolean
  status?: ListingStatus
}

export interface UpdateListingRequest {
  operationType: ListingOperationType
  price: number
  currency: ListingCurrency
  expenses: number | null
  title: string
  isFeatured: boolean
  status: ListingStatus
}
