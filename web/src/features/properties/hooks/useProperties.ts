import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { propertyService } from '../services/propertyService'
import type { CreatePropertyRequest, UpdatePropertyRequest } from '../types/property.types'

const KEY = ['properties']

export function useProperties() {
  return useQuery({ queryKey: KEY, queryFn: propertyService.list })
}

export function useCreateProperty() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (req: CreatePropertyRequest) => propertyService.create(req),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}

export function useUpdateProperty() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, req }: { id: string; req: UpdatePropertyRequest }) =>
      propertyService.update(id, req),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}

export function useDeleteProperty() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => propertyService.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}
