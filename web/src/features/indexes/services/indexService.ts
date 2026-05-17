import { api } from '@/shared/lib/api'
import type { IndexType, IndexValueDto, SyncIndexRequest, SyncIndexResult } from '../types/index.types'

export const indexService = {
  async list(type: IndexType, from: string, to: string): Promise<IndexValueDto[]> {
    const { data } = await api.get<IndexValueDto[]>('/indexes', {
      params: { type, from, to },
    })
    return data
  },
  async sync(req: SyncIndexRequest): Promise<SyncIndexResult> {
    const { data } = await api.post<SyncIndexResult>('/indexes/sync', req)
    return data
  },
}
