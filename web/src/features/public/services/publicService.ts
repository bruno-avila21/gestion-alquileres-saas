import { publicApi } from '@/shared/lib/publicApi'
import type {
  CreatePublicLeadRequest, PublicListingDetail, PublicListingFilters, PublicListingsResponse, PublicOrgDto,
} from '../types/public.types'

const DEFAULT_PAGE_SIZE = 24

/** Arma los query params del listado omitiendo todo valor vacío/undefined. */
function buildListingsParams(filters: PublicListingFilters): URLSearchParams {
  const params = new URLSearchParams()
  const set = (key: string, value: string | number | boolean | undefined) => {
    if (value === undefined || value === null || value === '') return
    params.append(key, String(value))
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
  set('page', filters.page ?? 1)
  set('pageSize', filters.pageSize ?? DEFAULT_PAGE_SIZE)
  filters.features?.forEach((f) => params.append('features', f))

  return params
}

export const publicService = {
  getOrg: (slug: string) =>
    publicApi.get<PublicOrgDto>(`/public/${slug}`).then((r) => r.data),

  getListings: (slug: string, filters: PublicListingFilters) =>
    publicApi
      .get<PublicListingsResponse>(`/public/${slug}/listings`, { params: buildListingsParams(filters) })
      .then((r) => r.data),

  getListing: (slug: string, id: string) =>
    publicApi.get<PublicListingDetail>(`/public/${slug}/listings/${id}`).then((r) => r.data),

  /**
   * 201 con `{ id }` si se creó la consulta; 204 sin cuerpo si el honeypot detectó un bot.
   * El formulario trata ambas respuestas igual (éxito), a propósito: no hay que revelarle al
   * bot que fue descartado.
   */
  createLead: (slug: string, body: CreatePublicLeadRequest) =>
    publicApi.post<{ id: string } | undefined>(`/public/${slug}/leads`, body).then(() => undefined),
}
