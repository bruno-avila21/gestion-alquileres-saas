import { useQuery } from '@tanstack/react-query'
import { indexService } from '../services/indexService'
import type { IndexType } from '../types/index.types'

export function useIndexes(type: IndexType, from: string, to: string) {
  return useQuery({
    queryKey: ['indexes', type, from, to],
    queryFn: () => indexService.list(type, from, to),
    staleTime: 30_000,
    enabled: !!from && !!to,
  })
}
