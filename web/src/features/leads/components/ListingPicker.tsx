import { useState } from 'react'
import { useListingOptions } from '../hooks/useListingOptions'

interface ListingPickerProps {
  value: string | undefined
  onChange: (listingId: string | undefined) => void
}

/** Selector de publicación opcional para la carga manual de una consulta ("Nueva consulta"). */
export function ListingPicker({ value, onChange }: ListingPickerProps) {
  const { data: listings, isLoading } = useListingOptions()
  const [query, setQuery] = useState('')

  const selected = listings?.find((l) => l.id === value)

  if (selected) {
    return (
      <div className="row between" style={{ border: '1px solid var(--hairline-2)', borderRadius: 'var(--r-3)', padding: '8px 10px' }}>
        <div style={{ minWidth: 0 }}>
          <div style={{ fontSize: 'var(--fs-sm)', fontWeight: 500, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
            {selected.title}
          </div>
          <div className="muted" style={{ fontSize: 'var(--fs-xs)' }}>{selected.propertyAddress}</div>
        </div>
        <button type="button" className="btn btn--ghost btn--sm" onClick={() => onChange(undefined)}>Quitar</button>
      </div>
    )
  }

  const q = query.trim().toLowerCase()
  const filtered = (listings ?? [])
    .filter((l) =>
      !q
      || l.title.toLowerCase().includes(q)
      || l.propertyAddress.toLowerCase().includes(q)
      || (l.propertyCode ?? '').toLowerCase().includes(q))
    .slice(0, 30)

  return (
    <div>
      <input
        className="input"
        placeholder="Buscar por título, dirección o código…"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
      />
      {query.trim() && (
        <div className="listing-picker-results">
          {isLoading ? (
            <div className="muted" style={{ padding: 10, fontSize: 'var(--fs-xs)' }}>Buscando…</div>
          ) : filtered.length === 0 ? (
            <div className="muted" style={{ padding: 10, fontSize: 'var(--fs-xs)' }}>Sin resultados</div>
          ) : (
            filtered.map((l) => (
              <button
                type="button"
                key={l.id}
                className="listing-picker-item"
                onClick={() => { onChange(l.id); setQuery('') }}
              >
                <b>{l.title}</b>
                <span className="muted">{l.propertyAddress}</span>
              </button>
            ))
          )}
        </div>
      )}
    </div>
  )
}
