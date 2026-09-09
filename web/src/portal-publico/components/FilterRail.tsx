import type { FacetDto, PublicListingFacets, PublicListingFilters, PublicOperationType } from '@/features/public/types/public.types'
import { PROPERTY_TYPE_LABELS } from '@/features/public/utils/labels'
import { CloseIcon } from './icons'

type Patch = Partial<PublicListingFilters>

interface FilterRailProps {
  facets: PublicListingFacets | undefined
  filters: PublicListingFilters
  onPatch: (patch: Patch) => void
  onClear: () => void
  open: boolean
  onClose: () => void
}

function sortNumeric(facets: FacetDto[]): FacetDto[] {
  return [...facets].sort((a, b) => Number(a.value) - Number(b.value))
}

/** Fila de facet de selección única (radio visual sobre un checkbox controlado). */
function SingleFacetRow({
  label, count, checked, onToggle,
}: { label: string; count: number; checked: boolean; onToggle: () => void }) {
  return (
    <label className="facet">
      <input type="checkbox" checked={checked} onChange={onToggle} />
      <span>{label}</span>
      <span className="cnt">{count}</span>
    </label>
  )
}

export function FilterRail({ facets, filters, onPatch, onClear, open, onClose }: FilterRailProps) {
  const operationOptions: { value: '' | PublicOperationType; label: string }[] = [
    { value: '', label: 'Todas' },
    { value: 'Sale', label: 'Venta' },
    { value: 'Rent', label: 'Alquiler' },
    { value: 'TemporaryRent', label: 'Temporario' },
  ]

  const creditCount = facets?.suitableForCredit.find((f) => f.value === 'true')?.count ?? 0

  return (
    <aside className={`filters${open ? ' open' : ''}`} aria-label="Filtros de búsqueda">
      <div className="filters-h">
        <h3>Filtros</h3>
        <button type="button" onClick={onClear}>Limpiar</button>
        <button type="button" className="close-f" aria-label="Cerrar filtros" onClick={onClose}>
          <CloseIcon />
        </button>
      </div>

      <div className="fgroup">
        <label>Operación</label>
        <div className="seg">
          {operationOptions.map((opt) => (
            <button
              key={opt.value || 'all'}
              type="button"
              className={(filters.operation ?? '') === opt.value ? 'on' : undefined}
              onClick={() => onPatch({ operation: opt.value || undefined, page: 1 })}
            >
              {opt.label}
            </button>
          ))}
        </div>
      </div>

      <div className="fgroup">
        <label>Tipo de propiedad</label>
        {facets?.propertyTypes.length ? (
          facets.propertyTypes.map((f) => (
            <SingleFacetRow
              key={f.value}
              label={PROPERTY_TYPE_LABELS[f.value as keyof typeof PROPERTY_TYPE_LABELS] ?? f.value}
              count={f.count}
              checked={filters.type === f.value}
              onToggle={() => onPatch({ type: filters.type === f.value ? undefined : (f.value as PublicListingFilters['type']), page: 1 })}
            />
          ))
        ) : (
          <span className="facet-empty">—</span>
        )}
      </div>

      {facets && facets.cities.length > 1 ? (
        <div className="fgroup">
          <label>Ciudad</label>
          {facets.cities.map((f) => (
            <SingleFacetRow
              key={f.value}
              label={f.value}
              count={f.count}
              checked={filters.city === f.value}
              onToggle={() => onPatch({ city: filters.city === f.value ? undefined : f.value, page: 1 })}
            />
          ))}
        </div>
      ) : null}

      <div className="fgroup">
        <label>Zona</label>
        {facets?.neighborhoods.length ? (
          facets.neighborhoods.map((f) => (
            <SingleFacetRow
              key={f.value}
              label={f.value}
              count={f.count}
              checked={filters.neighborhood === f.value}
              onToggle={() => onPatch({ neighborhood: filters.neighborhood === f.value ? undefined : f.value, page: 1 })}
            />
          ))
        ) : (
          <span className="facet-empty">—</span>
        )}
      </div>

      <div className="fgroup">
        <label>Precio</label>
        <div className="seg">
          {(['', 'USD', 'ARS'] as const).map((cur) => (
            <button
              key={cur || 'all'}
              type="button"
              className={(filters.currency ?? '') === cur ? 'on' : undefined}
              onClick={() => onPatch({ currency: cur || undefined, page: 1 })}
            >
              {cur || 'Todas'}
            </button>
          ))}
        </div>
        <div className="price-inputs">
          <input
            type="number"
            inputMode="numeric"
            placeholder="Mín"
            aria-label="Precio mínimo"
            value={filters.minPrice ?? ''}
            onChange={(e) => onPatch({ minPrice: e.target.value ? Number(e.target.value) : undefined, page: 1 })}
          />
          <span style={{ color: 'var(--faint)' }}>—</span>
          <input
            type="number"
            inputMode="numeric"
            placeholder="Máx"
            aria-label="Precio máximo"
            value={filters.maxPrice ?? ''}
            onChange={(e) => onPatch({ maxPrice: e.target.value ? Number(e.target.value) : undefined, page: 1 })}
          />
        </div>
      </div>

      <div className="fgroup">
        <label>Ambientes</label>
        {facets?.rooms.length ? (
          sortNumeric(facets.rooms).map((f) => (
            <SingleFacetRow
              key={f.value}
              label={`${f.value}+ ambientes`}
              count={f.count}
              checked={filters.minRooms === Number(f.value)}
              onToggle={() => onPatch({ minRooms: filters.minRooms === Number(f.value) ? undefined : Number(f.value), page: 1 })}
            />
          ))
        ) : (
          <span className="facet-empty">—</span>
        )}
      </div>

      <div className="fgroup">
        <label>Dormitorios</label>
        {facets?.bedrooms.length ? (
          sortNumeric(facets.bedrooms).map((f) => (
            <SingleFacetRow
              key={f.value}
              label={`${f.value}+ dormitorios`}
              count={f.count}
              checked={filters.minBedrooms === Number(f.value)}
              onToggle={() => onPatch({ minBedrooms: filters.minBedrooms === Number(f.value) ? undefined : Number(f.value), page: 1 })}
            />
          ))
        ) : (
          <span className="facet-empty">—</span>
        )}
      </div>

      <div className="fgroup">
        <label>Superficie (m²)</label>
        <div className="price-inputs">
          <input
            type="number"
            inputMode="numeric"
            placeholder="Mín"
            aria-label="Superficie mínima"
            value={filters.minArea ?? ''}
            onChange={(e) => onPatch({ minArea: e.target.value ? Number(e.target.value) : undefined, page: 1 })}
          />
          <span style={{ color: 'var(--faint)' }}>—</span>
          <input
            type="number"
            inputMode="numeric"
            placeholder="Máx"
            aria-label="Superficie máxima"
            value={filters.maxArea ?? ''}
            onChange={(e) => onPatch({ maxArea: e.target.value ? Number(e.target.value) : undefined, page: 1 })}
          />
        </div>
      </div>

      <div className="fgroup">
        <label>Características</label>
        {facets?.features.length ? (
          facets.features.map((f) => (
            <label className="facet" key={f.value}>
              <input
                type="checkbox"
                checked={filters.features?.includes(f.value) ?? false}
                onChange={() => {
                  const current = filters.features ?? []
                  const next = current.includes(f.value) ? current.filter((v) => v !== f.value) : [...current, f.value]
                  onPatch({ features: next, page: 1 })
                }}
              />
              <span>{f.value}</span>
              <span className="cnt">{f.count}</span>
            </label>
          ))
        ) : (
          <span className="facet-empty">—</span>
        )}
      </div>

      {creditCount > 0 ? (
        <div className="fgroup">
          <label>Financiación</label>
          <label className="facet">
            <input
              type="checkbox"
              checked={filters.credit === true}
              onChange={() => onPatch({ credit: filters.credit ? undefined : true, page: 1 })}
            />
            <span>Apto crédito</span>
            <span className="cnt">{creditCount}</span>
          </label>
        </div>
      ) : null}
    </aside>
  )
}
