import { useMutation, useQueryClient } from '@tanstack/react-query'
import { indexService } from '../services/indexService'
import type { SyncIndexRequest } from '../types/index.types'

export function useSyncIndex() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (req: SyncIndexRequest) => indexService.sync(req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['indexes'] })
    },
  })
}
