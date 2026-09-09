import type { PublicListingFilters } from '@/features/public/types/public.types'
import { operationLabel, propertyTypeLabel } from '@/features/public/utils/labels'

interface Chip {
  key: string
  label: string
  onRemove: () => void
}

export function FilterChips({ filters, onPatch }: { filters: PublicListingFilters; onPatch: (patch: Partial<PublicListingFilters>) => void }) {
  const chips: Chip[] = []

  if (filters.operation) chips.push({ key: 'operation', label: operationLabel(filters.operation), onRemove: () => onPatch({ operation: undefined, page: 1 }) })
  if (filters.type) chips.push({ key: 'type', label: propertyTypeLabel(filters.type), onRemove: () => onPatch({ type: undefined, page: 1 }) })
  if (filters.city) chips.push({ key: 'city', label: filters.city, onRemove: () => onPatch({ city: undefined, page: 1 }) })
  if (filters.neighborhood) chips.push({ key: 'neighborhood', label: filters.neighborhood, onRemove: () => onPatch({ neighborhood: undefined, page: 1 }) })
  if (filters.currency) chips.push({ key: 'currency', label: filters.currency, onRemove: () => onPatch({ currency: undefined, page: 1 }) })
  if (filters.minPrice || filters.maxPrice) {
    const label = `${filters.minPrice ? `desde ${filters.minPrice}` : ''}${filters.minPrice && filters.maxPrice ? ' — ' : ''}${filters.maxPrice ? `hasta ${filters.maxPrice}` : ''}`
    chips.push({ key: 'price', label, onRemove: () => onPatch({ minPrice: undefined, maxPrice: undefined, page: 1 }) })
  }
  if (filters.minRooms) chips.push({ key: 'rooms', label: `${filters.minRooms}+ amb`, onRemove: () => onPatch({ minRooms: undefined, page: 1 }) })
  if (filters.minBedrooms) chips.push({ key: 'bedrooms', label: `${filters.minBedrooms}+ dorm`, onRemove: () => onPatch({ minBedrooms: undefined, page: 1 }) })
  if (filters.minArea || filters.maxArea) {
    const label = `${filters.minArea ? `desde ${filters.minArea}m²` : ''}${filters.minArea && filters.maxArea ? ' — ' : ''}${filters.maxArea ? `hasta ${filters.maxArea}m²` : ''}`
    chips.push({ key: 'area', label, onRemove: () => onPatch({ minArea: undefined, maxArea: undefined, page: 1 }) })
  }
  filters.features?.forEach((f) => chips.push({
    key: `feat-${f}`,
    label: f,
    onRemove: () => onPatch({ features: (filters.features ?? []).filter((v) => v !== f), page: 1 }),
  }))
  if (filters.credit) chips.push({ key: 'credit', label: 'Apto crédito', onRemove: () => onPatch({ credit: undefined, page: 1 }) })

  if (!chips.length) return null

  return (
    <div className="chips">
      {chips.map((chip) => (
        <span className="chip" key={chip.key}>
          {chip.label}
          <button type="button" aria-label={`Quitar filtro ${chip.label}`} onClick={chip.onRemove}>×</button>
        </span>
      ))}
    </div>
  )
}
