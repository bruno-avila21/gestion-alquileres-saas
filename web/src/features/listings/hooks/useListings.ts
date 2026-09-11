import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { listingService } from '../services/listingService'
import type { CreateListingRequest, UpdateListingRequest } from '../types/listing.types'

const KEY = ['listings']

export function useListingsByProperty(propertyId: string) {
  return useQuery({
    queryKey: [...KEY, propertyId],
    queryFn: () => listingService.listByProperty(propertyId),
    enabled: !!propertyId,
  })
}

export function useCreateListing() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (req: CreateListingRequest) => listingService.create(req),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}

export function useUpdateListing() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, req }: { id: string; req: UpdateListingRequest }) =>
      listingService.update(id, req),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}

export function useDeleteListing() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => listingService.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}
