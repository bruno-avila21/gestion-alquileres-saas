export type IndexType = 'ICL' | 'IPC'

export interface IndexValueDto {
  id: string
  indexType: IndexType
  period: string        // yyyy-MM-dd
  value: number
  variationPct: number | null
  source: string        // 'BCRA' | 'INDEC' | fallback strings
  fetchedAt: string     // ISO 8601
}

export interface SyncIndexRequest {
  indexType: IndexType
  period: string        // yyyy-MM-dd, first day of month
}

export interface SyncIndexResult {
  success: boolean
  wasFallback: boolean
  alreadyExisted: boolean
  message: string | null
  indexValue: IndexValueDto
}

export interface ApiErrorBody {
  error?: string
  errors?: { field: string; message: string }[]
}
