import { useQuery } from '@tanstack/react-query'
import { api } from '@/shared/lib/api'
import type { ListingDto } from '@/features/listings/types/listing.types'

/**
 * Todas las publicaciones de la organización, para el selector de "Nueva consulta" (carga manual).
 * `GET /listings` sin `propertyId` devuelve el listado completo — mismo endpoint que usa la pestaña
 * de publicaciones de una propiedad, sin el filtro por propiedad.
 */
export function useListingOptions() {
  return useQuery({
    queryKey: ['listings', 'all'],
    queryFn: async () => {
      const { data } = await api.get<ListingDto[]>('/listings')
      return data
    },
    staleTime: 60_000,
  })
}
