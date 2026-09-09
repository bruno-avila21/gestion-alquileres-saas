import type { PublicSortOption } from '@/features/public/types/public.types'
import { SORT_LABELS } from '@/features/public/utils/labels'
import { FilterIcon } from './icons'

const SORT_OPTIONS: ('' | PublicSortOption)[] = ['', 'price_asc', 'price_desc', 'rooms_desc', 'rooms_asc', 'newest']

export function SortBar({
  total, sort, onSortChange, onToggleFilters,
}: {
  total: number
  sort: PublicSortOption | undefined
  onSortChange: (sort: PublicSortOption | undefined) => void
  onToggleFilters: () => void
}) {
  return (
    <div className="results-top">
      <div className="count">
        <b>{total}</b> propiedad{total === 1 ? '' : 'es'}
      </div>
      <div style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
        <button type="button" className="filter-toggle" onClick={onToggleFilters}>
          <FilterIcon />
          Filtros
        </button>
        <div className="sortsel">
          <label htmlFor="pp-sort">Ordenar</label>
          <select
            id="pp-sort"
            value={sort ?? ''}
            onChange={(e) => onSortChange(e.target.value ? (e.target.value as PublicSortOption) : undefined)}
          >
            {SORT_OPTIONS.map((opt) => (
              <option key={opt || 'feat'} value={opt}>{SORT_LABELS[opt]}</option>
            ))}
          </select>
        </div>
      </div>
    </div>
  )
}
