import type {
  PublicCurrency, PublicListingFilters, PublicOperationType, PublicPropertyType, PublicSortOption,
} from '../types/public.types'

const OPERATIONS: PublicOperationType[] = ['Sale', 'Rent', 'TemporaryRent']
const PROPERTY_TYPES: PublicPropertyType[] = ['House', 'Apartment', 'Commercial', 'Land', 'Other', 'PH', 'Office']
const CURRENCIES: PublicCurrency[] = ['ARS', 'USD']
const SORTS: PublicSortOption[] = ['price_asc', 'price_desc', 'rooms_asc', 'rooms_desc', 'newest']

function toNumber(value: string | null): number | undefined {
  if (!value) return undefined
  const n = Number(value)
  return Number.isFinite(n) ? n : undefined
}

/** Lee los filtros desde los searchParams de la URL del listado (`/propiedades?...`). */
export function parseListingFilters(params: URLSearchParams): PublicListingFilters {
  const operation = params.get('operation')
  const type = params.get('type')
  const currency = params.get('currency')
  const sort = params.get('sort')

  return {
    operation: operation && OPERATIONS.includes(operation as PublicOperationType) ? (operation as PublicOperationType) : undefined,
    type: type && PROPERTY_TYPES.includes(type as PublicPropertyType) ? (type as PublicPropertyType) : undefined,
    city: params.get('city') ?? undefined,
    neighborhood: params.get('neighborhood') ?? undefined,
    currency: currency && CURRENCIES.includes(currency as PublicCurrency) ? (currency as PublicCurrency) : undefined,
    minPrice: toNumber(params.get('minPrice')),
    maxPrice: toNumber(params.get('maxPrice')),
    minRooms: toNumber(params.get('minRooms')),
    minBedrooms: toNumber(params.get('minBedrooms')),
    minArea: toNumber(params.get('minArea')),
    maxArea: toNumber(params.get('maxArea')),
    features: params.getAll('features'),
    credit: params.get('credit') === 'true' ? true : undefined,
    sort: sort && SORTS.includes(sort as PublicSortOption) ? (sort as PublicSortOption) : undefined,
    page: toNumber(params.get('page')) ?? 1,
    pageSize: toNumber(params.get('pageSize')) ?? 24,
  }
}

/** Vuelca los filtros a la URL (para que el listado sea compartible por link). Omite lo vacío. */
export function serializeListingFilters(filters: PublicListingFilters): URLSearchParams {
  const params = new URLSearchParams()
  const set = (key: string, value: string | number | boolean | undefined) => {
    if (value === undefined || value === null || value === '') return
    params.set(key, String(value))
  }

  set('operation', filters.operation)
  set('type', filters.type)
  set('city', filters.city)
  set('neighborhood', filters.neighborhood)
  set('currency', filters.currency)
  set('minPrice', filters.minPrice)
  set('maxPrice', filters.maxPrice)
  set('minRooms', filters.minRooms)
  set('minBedrooms', filters.minBedrooms)
  set('minArea', filters.minArea)
  set('maxArea', filters.maxArea)
  set('credit', filters.credit)
  set('sort', filters.sort)
  if (filters.page && filters.page > 1) params.set('page', String(filters.page))
  filters.features?.forEach((f) => params.append('features', f))

  return params
}
