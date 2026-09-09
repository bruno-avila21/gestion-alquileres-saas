import { useState } from 'react'

const FEATURE_CATALOG = [
  'Agua corriente', 'Cloaca', 'Gas natural', 'Electricidad', 'Pavimento',
  'Aire acondicionado', 'Calefacción', 'Parrilla', 'Pileta', 'SUM',
  'Balcón', 'Terraza', 'Patio', 'Jardín', 'Quincho', 'Lavadero',
  'Ascensor', 'Seguridad', 'Apto mascotas', 'Apto profesional', 'Amoblado',
]

const MAX_LENGTH = 40

interface FeaturesEditorProps {
  value: string[]
  onChange: (features: string[]) => void
}

/** Chips de catálogo + característica libre, para la ficha pública de una propiedad. */
export function FeaturesEditor({ value, onChange }: FeaturesEditorProps) {
  const [custom, setCustom] = useState('')
  const [err, setErr] = useState('')

  function toggle(feature: string) {
    onChange(
      value.includes(feature) ? value.filter((f) => f !== feature) : [...value, feature],
    )
  }

  function addCustom() {
    const trimmed = custom.trim()
    setErr('')
    if (!trimmed) return
    if (trimmed.length > MAX_LENGTH) {
      setErr(`Máximo ${MAX_LENGTH} caracteres.`)
      return
    }
    if (trimmed.includes('|')) {
      setErr('No puede contener el carácter "|".')
      return
    }
    if (value.some((f) => f.toLowerCase() === trimmed.toLowerCase())) {
      setCustom('')
      return
    }
    onChange([...value, trimmed])
    setCustom('')
  }

  const extra = value.filter((f) => !FEATURE_CATALOG.includes(f))

  return (
    <div>
      <label className="label">Características</label>
      <div className="row" style={{ flexWrap: 'wrap', gap: 6, marginTop: 6 }}>
        {FEATURE_CATALOG.map((f) => {
          const active = value.includes(f)
          return (
            <button
              key={f}
              type="button"
              className={`chip${active ? ' chip--info' : ''}`}
              style={{ cursor: 'pointer', borderStyle: active ? 'solid' : 'dashed' }}
              onClick={() => toggle(f)}
              aria-pressed={active}
            >
              {f}
            </button>
          )
        })}
        {extra.map((f) => (
          <button
            key={f}
            type="button"
            className="chip chip--info"
            style={{ cursor: 'pointer' }}
            onClick={() => toggle(f)}
            aria-pressed="true"
            title="Quitar característica"
          >
            {f} ×
          </button>
        ))}
      </div>
      <label className="label" htmlFor="ficha-feature-custom" style={{ display: 'block', marginTop: 10 }}>
        Agregar otra
      </label>
      <div className="row" style={{ gap: 6, marginTop: 4 }}>
        <input
          id="ficha-feature-custom"
          className="input"
          style={{ flex: 1 }}
          placeholder="Ej: Vista al río"
          value={custom}
          maxLength={MAX_LENGTH}
          onChange={(e) => setCustom(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') { e.preventDefault(); addCustom() }
          }}
        />
        <button type="button" className="btn btn--sm" onClick={addCustom}>Agregar</button>
      </div>
      {err && <div role="alert" style={{ fontSize: 'var(--fs-xs)', color: 'var(--danger)', marginTop: 4 }}>{err}</div>}
    </div>
  )
}
