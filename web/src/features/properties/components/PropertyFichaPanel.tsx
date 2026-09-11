import { useState } from 'react'
import type { PropertyDto } from '../types/property.types'
import { PropertyPhotosTab } from './PropertyPhotosTab'
import { PropertyListingsTab } from './PropertyListingsTab'

type FichaTab = 'fotos' | 'publicaciones'

const TABS: { k: FichaTab; lbl: string }[] = [
  { k: 'fotos', lbl: 'Fotos' },
  { k: 'publicaciones', lbl: 'Publicaciones' },
]

/** Panel expandido bajo una fila de propiedad, con pestañas Fotos / Publicaciones. */
export function PropertyFichaPanel({ property, onClose }: { property: PropertyDto; onClose: () => void }) {
  const [tab, setTab] = useState<FichaTab>('fotos')

  return (
    <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
      <div className="between" style={{ padding: '8px 10px 8px 14px', borderBottom: '1px solid var(--hairline)' }}>
        <div style={{ display: 'flex', gap: 4 }}>
          {TABS.map((t) => (
            <button
              key={t.k}
              role="tab"
              aria-selected={tab === t.k}
              onClick={() => setTab(t.k)}
              style={{
                padding: '6px 10px', borderRadius: 6, border: 'none',
                background: tab === t.k ? 'var(--surface-3)' : 'transparent',
                color: tab === t.k ? 'var(--ink)' : 'var(--muted)',
                fontWeight: 500, fontSize: 'var(--fs-sm)', cursor: 'pointer',
                fontFamily: 'inherit',
              }}
            >
              {t.lbl}
            </button>
          ))}
        </div>
        <button className="btn btn--ghost btn--sm btn--icon" onClick={onClose} aria-label="Cerrar ficha" title="Cerrar">
          ×
        </button>
      </div>
      <div style={{ padding: 14 }}>
        {tab === 'fotos'
          ? <PropertyPhotosTab propertyId={property.id} />
          : <PropertyListingsTab property={property} />}
      </div>
    </div>
  )
}
