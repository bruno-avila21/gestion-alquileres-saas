import { keepPreviousData, useMutation, useQuery } from '@tanstack/react-query'
import { publicService } from '../services/publicService'
import type { CreatePublicLeadRequest, PublicListingFilters } from '../types/public.types'

export const PUBLIC_ORG_KEY = ['public-org'] as const
export const PUBLIC_LISTINGS_KEY = ['public-listings'] as const
export const PUBLIC_LISTING_KEY = ['public-listing'] as const

export function usePublicOrg(slug: string | undefined) {
  return useQuery({
    queryKey: [...PUBLIC_ORG_KEY, slug],
    queryFn: () => publicService.getOrg(slug!),
    enabled: !!slug,
    retry: false,
    staleTime: 5 * 60_000,
  })
}

export function usePublicListings(slug: string | undefined, filters: PublicListingFilters) {
  return useQuery({
    queryKey: [...PUBLIC_LISTINGS_KEY, slug, filters],
    queryFn: () => publicService.getListings(slug!, filters),
    enabled: !!slug,
    placeholderData: keepPreviousData,
  })
}

export function usePublicListing(slug: string | undefined, id: string | undefined) {
  return useQuery({
    queryKey: [...PUBLIC_LISTING_KEY, slug, id],
    queryFn: () => publicService.getListing(slug!, id!),
    enabled: !!slug && !!id,
    retry: false,
  })
}

/**
 * Alta de una consulta desde el sitio público (ficha o "Contacto" de la home). Anónimo: no hay
 * cache de servidor que invalidar, así que no participa del ciclo de queries del resto de `public`.
 */
export function useCreatePublicLead(slug: string | undefined) {
  return useMutation({
    mutationFn: (body: CreatePublicLeadRequest) => publicService.createLead(slug!, body),
  })
}
