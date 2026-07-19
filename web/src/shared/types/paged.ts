/** Server-side pagination envelope, mirrors the API PagedResult<T> (audit M10). */
export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}
