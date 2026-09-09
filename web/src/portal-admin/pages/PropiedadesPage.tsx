import { useState } from 'react'
import { useSearchParams } from 'react-router'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { useProperties, useCreateProperty, useUpdateProperty, useDeleteProperty } from '@/features/properties/hooks/useProperties'
import type { PropertyDto, PropertyType } from '@/features/properties/types/property.types'
import { FeaturesEditor } from '@/features/properties/components/FeaturesEditor'
import { PropertyFichaPanel } from '@/features/properties/components/PropertyFichaPanel'
import { IcBuilding, IcPlus, IcEdit, IcArchive, IcDoc, IcChev, IcChevDown } from '@/shared/components/ui/Icons'
import { PaginationBar } from '@/shared/components/ui/PaginationBar'
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog'
import { QueryError } from '@/shared/components/ui/QueryError'

const PAGE_SIZE = 20

const PROPERTY_TYPES: { value: PropertyType; label: string }[] = [
  { value: 'Apartment', label: 'Departamento' },
  { value: 'House', label: 'Casa' },
  { value: 'PH', label: 'PH' },
  { value: 'Commercial', label: 'Local comercial' },
  { value: 'Office', label: 'Oficina' },
  { value: 'Land', label: 'Terreno' },
  { value: 'Other', label: 'Otro' },
]

const PROVINCES = [
  'CABA', 'Buenos Aires', 'Córdoba', 'Santa Fe', 'Mendoza',
  'Tucumán', 'Entre Ríos', 'Salta', 'Misiones', 'Chaco',
  'Corrientes', 'Santiago del Estero', 'San Juan', 'Jujuy', 'Río Negro',
  'Neuquén', 'Formosa', 'Chubut', 'San Luis', 'Catamarca',
  'La Rioja', 'La Pampa', 'Santa Cruz', 'Tierra del Fuego',
]

type SuitableForCreditOption = '' | 'true' | 'false'

type FormState = {
  address: string; city: string; province: string
  propertyType: PropertyType; areaM2: string; notes: string
  neighborhood: string; code: string; description: string
  rooms: string; bedrooms: string; bathrooms: string; garages: string; ageYears: string
  coveredAreaM2: string; latitude: string; longitude: string
  suitableForCredit: SuitableForCreditOption
  features: string[]
}

const EMPTY_FORM: FormState = {
  address: '', city: '', province: 'Buenos Aires',
  propertyType: 'Apartment', areaM2: '', notes: '',
  neighborhood: '', code: '', description: '',
  rooms: '', bedrooms: '', bathrooms: '', garages: '', ageYears: '',
  coveredAreaM2: '', latitude: '', longitude: '',
  suitableForCredit: '',
  features: [],
}

function toIntOrNull(v: string): number | null {
  if (!v.trim()) return null
  const n = parseInt(v, 10)
  return Number.isNaN(n) ? null : n
}

function toFloatOrNull(v: string): number | null {
  if (!v.trim()) return null
  const n = parseFloat(v)
  return Number.isNaN(n) ? null : n
}

export default function PropiedadesPage() {
  const { data: properties, isLoading, error } = useProperties()
  const create = useCreateProperty()
  const update = useUpdateProperty()
  const remove = useDeleteProperty()

  // `?highlight={propertyId}` llega desde el detalle de una consulta (features/leads) para abrir
  // directo la ficha de la propiedad asociada — esta página no tiene una ruta propia por id.
  const [searchParams] = useSearchParams()

  const [showForm, setShowForm] = useState(false)
  const [showFicha, setShowFicha] = useState(false)
  const [editing, setEditing] = useState<PropertyDto | null>(null)
  const [form, setForm] = useState<FormState>(EMPTY_FORM)
  const [search, setSearch] = useState('')
  const [err, setErr] = useState('')
  const [page, setPage] = useState(0)
  const [confirmArchive, setConfirmArchive] = useState<PropertyDto | null>(null)
  const [openFichaId, setOpenFichaId] = useState<string | null>(() => searchParams.get('highlight'))

  const filtered = (properties ?? []).filter(p =>
    p.address.toLowerCase().includes(search.toLowerCase()) ||
    p.city.toLowerCase().includes(search.toLowerCase())
  )

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE))
  const paginated = filtered.slice(page * PAGE_SIZE, (page + 1) * PAGE_SIZE)

  function openCreate() {
    setEditing(null)
    setForm(EMPTY_FORM)
    setErr('')
    setShowFicha(false)
    setShowForm(true)
  }

  function openEdit(p: PropertyDto) {
    setEditing(p)
    setForm({
      address: p.address, city: p.city, province: p.province,
      propertyType: p.propertyType, areaM2: p.areaM2?.toString() ?? '', notes: p.notes ?? '',
      neighborhood: p.neighborhood ?? '', code: p.code ?? '', description: p.description ?? '',
      rooms: p.rooms?.toString() ?? '', bedrooms: p.bedrooms?.toString() ?? '',
      bathrooms: p.bathrooms?.toString() ?? '', garages: p.garages?.toString() ?? '',
      ageYears: p.ageYears?.toString() ?? '', coveredAreaM2: p.coveredAreaM2?.toString() ?? '',
      latitude: p.latitude?.toString() ?? '', longitude: p.longitude?.toString() ?? '',
      suitableForCredit: p.suitableForCredit === null || p.suitableForCredit === undefined
        ? '' : (p.suitableForCredit ? 'true' : 'false'),
      features: p.features ?? [],
    })
    setErr('')
    setShowFicha(false)
    setShowForm(true)
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setErr('')
    const payload = {
      address: form.address.trim(),
      city: form.city.trim(),
      province: form.province,
      propertyType: form.propertyType,
      areaM2: toFloatOrNull(form.areaM2),
      notes: form.notes.trim() || null,
      details: {
        neighborhood: form.neighborhood.trim() || null,
        code: form.code.trim() || null,
        description: form.description.trim() || null,
        rooms: toIntOrNull(form.rooms),
        bedrooms: toIntOrNull(form.bedrooms),
        bathrooms: toIntOrNull(form.bathrooms),
        garages: toIntOrNull(form.garages),
        ageYears: toIntOrNull(form.ageYears),
        coveredAreaM2: toFloatOrNull(form.coveredAreaM2),
        latitude: toFloatOrNull(form.latitude),
        longitude: toFloatOrNull(form.longitude),
        suitableForCredit: form.suitableForCredit === '' ? null : form.suitableForCredit === 'true',
        features: form.features,
      },
    }
    try {
      if (editing) {
        await update.mutateAsync({ id: editing.id, req: { ...payload, isActive: editing.isActive } })
      } else {
        await create.mutateAsync(payload)
      }
      setShowForm(false)
    } catch {
      setErr('Error al guardar. Verificá los datos e intentá de nuevo.')
    }
  }

  async function doArchive() {
    const p = confirmArchive
    setConfirmArchive(null)
    if (p) await remove.mutateAsync(p.id)
  }

  return (
    <>
      <AdminTopbar
        crumbs={['Propiedades']}
        right={<button className="btn btn--sm btn--primary" onClick={openCreate}><IcPlus size={12} /> Nueva propiedad</button>}
      />
      <ConfirmDialog
        open={!!confirmArchive}
        title="Archivar propiedad"
        description={confirmArchive ? `"${confirmArchive.address}" quedará archivada y no aparecerá en los listados activos.` : ''}
        confirmLabel="Archivar"
        destructive
        onConfirm={doArchive}
        onCancel={() => setConfirmArchive(null)}
      />
    <div className="page">
      <div className="page-h">
        <div>
          <h1>Propiedades</h1>
          <div className="lead">Inmuebles administrados</div>
        </div>
      </div>

      <input
        className="input input--sm"
        style={{ width: 280 }}
        placeholder="Buscar por dirección o ciudad…"
        value={search}
        onChange={e => { setSearch(e.target.value); setPage(0) }}
      />

      {error && <QueryError message="Error al cargar propiedades." />}

      {showForm && (
        <div className="card" style={{ padding: 16, display: 'flex', flexDirection: 'column', gap: 14, maxWidth: 640 }}>
          <h2 style={{ fontWeight: 600, margin: 0 }}>{editing ? 'Editar propiedad' : 'Nueva propiedad'}</h2>
          {err && <div role="alert" style={{ fontSize: 'var(--fs-sm)', color: 'var(--danger)' }}>{err}</div>}
          <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            <div>
              <label className="label" htmlFor="prop-address">Dirección *</label>
              <input id="prop-address" className="input" value={form.address} onChange={e => setForm(f => ({ ...f, address: e.target.value }))} required />
            </div>
            <div className="grid-2">
              <div>
                <label className="label" htmlFor="prop-city">Ciudad *</label>
                <input id="prop-city" className="input" value={form.city} onChange={e => setForm(f => ({ ...f, city: e.target.value }))} required />
              </div>
              <div>
                <label className="label" htmlFor="prop-province">Provincia *</label>
                <select
                  id="prop-province"
                  className="select"
                  value={form.province}
                  onChange={e => setForm(f => ({ ...f, province: e.target.value }))}
                >
                  {PROVINCES.map(p => <option key={p}>{p}</option>)}
                </select>
              </div>
            </div>
            <div className="grid-2">
              <div>
                <label className="label" htmlFor="prop-type">Tipo *</label>
                <select
                  id="prop-type"
                  className="select"
                  value={form.propertyType}
                  onChange={e => setForm(f => ({ ...f, propertyType: e.target.value as PropertyType }))}
                >
                  {PROPERTY_TYPES.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
                </select>
              </div>
              <div>
                <label className="label" htmlFor="prop-area">Superficie (m²)</label>
                <input
                  id="prop-area"
                  className="input"
                  type="number"
                  min="1"
                  step="0.01"
                  value={form.areaM2}
                  onChange={e => setForm(f => ({ ...f, areaM2: e.target.value }))}
                />
              </div>
            </div>
            <div>
              <label className="label" htmlFor="prop-notes">Notas</label>
              <input id="prop-notes" className="input" value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} />
            </div>

            <div style={{ borderTop: '1px solid var(--hairline)', paddingTop: 12 }}>
              <button
                type="button"
                onClick={() => setShowFicha(v => !v)}
                aria-expanded={showFicha}
                aria-controls="ficha-publica-section"
                className="row"
                style={{ background: 'none', border: 'none', padding: 0, cursor: 'pointer', fontSize: 'var(--fs-sm)', fontWeight: 500, color: 'var(--ink)' }}
              >
                {showFicha ? <IcChevDown size={14} /> : <IcChev size={14} />}
                Ficha pública
              </button>

              {showFicha && (
                <div id="ficha-publica-section" style={{ display: 'flex', flexDirection: 'column', gap: 12, marginTop: 12 }}>
                  <div className="grid-2">
                    <div>
                      <label className="label" htmlFor="prop-neighborhood">Barrio</label>
                      <input id="prop-neighborhood" className="input" value={form.neighborhood} onChange={e => setForm(f => ({ ...f, neighborhood: e.target.value }))} />
                    </div>
                    <div>
                      <label className="label" htmlFor="prop-code">Código</label>
                      <input id="prop-code" className="input" value={form.code} onChange={e => setForm(f => ({ ...f, code: e.target.value }))} />
                    </div>
                  </div>

                  <div>
                    <label className="label" htmlFor="prop-description">Descripción</label>
                    <textarea
                      id="prop-description"
                      className="input"
                      style={{ height: 80, padding: '8px 12px', resize: 'vertical' }}
                      value={form.description}
                      onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
                    />
                  </div>

                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 'var(--s-7)' }}>
                    <div>
                      <label className="label" htmlFor="prop-rooms">Ambientes</label>
                      <input id="prop-rooms" className="input" type="number" min="0" step="1" value={form.rooms} onChange={e => setForm(f => ({ ...f, rooms: e.target.value }))} />
                    </div>
                    <div>
                      <label className="label" htmlFor="prop-bedrooms">Dormitorios</label>
                      <input id="prop-bedrooms" className="input" type="number" min="0" step="1" value={form.bedrooms} onChange={e => setForm(f => ({ ...f, bedrooms: e.target.value }))} />
                    </div>
                    <div>
                      <label className="label" htmlFor="prop-bathrooms">Baños</label>
                      <input id="prop-bathrooms" className="input" type="number" min="0" step="1" value={form.bathrooms} onChange={e => setForm(f => ({ ...f, bathrooms: e.target.value }))} />
                    </div>
                    <div>
                      <label className="label" htmlFor="prop-garages">Cocheras</label>
                      <input id="prop-garages" className="input" type="number" min="0" step="1" value={form.garages} onChange={e => setForm(f => ({ ...f, garages: e.target.value }))} />
                    </div>
                  </div>

                  <div className="grid-2">
                    <div>
                      <label className="label" htmlFor="prop-age">Antigüedad (años)</label>
                      <input id="prop-age" className="input" type="number" min="0" step="1" value={form.ageYears} onChange={e => setForm(f => ({ ...f, ageYears: e.target.value }))} />
                    </div>
                    <div>
                      <label className="label" htmlFor="prop-covered-area">m² cubiertos</label>
                      <input id="prop-covered-area" className="input" type="number" min="0" step="0.01" value={form.coveredAreaM2} onChange={e => setForm(f => ({ ...f, coveredAreaM2: e.target.value }))} />
                    </div>
                  </div>

                  <div>
                    <label className="label" htmlFor="prop-credit">Apto crédito</label>
                    <select
                      id="prop-credit"
                      className="select"
                      value={form.suitableForCredit}
                      onChange={e => setForm(f => ({ ...f, suitableForCredit: e.target.value as SuitableForCreditOption }))}
                    >
                      <option value="">Sin especificar</option>
                      <option value="true">Sí</option>
                      <option value="false">No</option>
                    </select>
                  </div>

                  <div className="grid-2">
                    <div>
                      <label className="label" htmlFor="prop-lat">Latitud</label>
                      <input id="prop-lat" className="input" type="number" step="0.000001" value={form.latitude} onChange={e => setForm(f => ({ ...f, latitude: e.target.value }))} />
                    </div>
                    <div>
                      <label className="label" htmlFor="prop-lng">Longitud</label>
                      <input id="prop-lng" className="input" type="number" step="0.000001" value={form.longitude} onChange={e => setForm(f => ({ ...f, longitude: e.target.value }))} />
                    </div>
                  </div>

                  <FeaturesEditor value={form.features} onChange={features => setForm(f => ({ ...f, features }))} />
                </div>
              )}
            </div>

            <div className="row" style={{ gap: 8, justifyContent: 'flex-end' }}>
              <button type="button" className="btn btn--sm" onClick={() => setShowForm(false)}>Cancelar</button>
              <button type="submit" className="btn btn--sm btn--primary" disabled={create.isPending || update.isPending}>
                {editing ? 'Guardar cambios' : 'Crear propiedad'}
              </button>
            </div>
          </form>
        </div>
      )}

      {isLoading ? (
        <div className="card" style={{ padding: 48, textAlign: 'center', color: 'var(--muted)' }}>Cargando…</div>
      ) : filtered.length === 0 ? (
        <div className="card" style={{ padding: 48, textAlign: 'center', color: 'var(--muted)' }}>
          <IcBuilding size={32} style={{ margin: '0 auto 8px', display: 'block' }} />
          No hay propiedades.
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          {paginated.map(p => (
            <div key={p.id} style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
              <div className="card row" style={{ padding: '12px 14px', gap: 12 }}>
                <IcBuilding size={20} style={{ color: 'var(--brand)', flexShrink: 0 }} />
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ fontWeight: 500 }}>{p.address}</div>
                  <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>
                    {p.city}, {p.province} · {PROPERTY_TYPES.find(t => t.value === p.propertyType)?.label ?? p.propertyType}
                    {p.areaM2 ? ` · ${p.areaM2} m²` : ''}
                  </div>
                </div>
                {!p.isActive && (
                  <span className="chip chip--warn" style={{ height: 20, fontSize: 10 }}>Archivada</span>
                )}
                <button
                  className="btn btn--ghost btn--sm btn--icon"
                  onClick={() => setOpenFichaId(id => id === p.id ? null : p.id)}
                  aria-label="Ver ficha pública"
                  aria-expanded={openFichaId === p.id}
                  title="Ficha"
                >
                  <IcDoc size={14} />
                </button>
                <button className="btn btn--ghost btn--sm btn--icon" onClick={() => openEdit(p)} aria-label="Editar propiedad" title="Editar">
                  <IcEdit size={14} />
                </button>
                {p.isActive && (
                  <button className="btn btn--ghost btn--sm btn--icon" onClick={() => setConfirmArchive(p)} aria-label="Archivar propiedad" title="Archivar">
                    <IcArchive size={14} />
                  </button>
                )}
              </div>
              {openFichaId === p.id && (
                <PropertyFichaPanel property={p} onClose={() => setOpenFichaId(null)} />
              )}
            </div>
          ))}
        </div>
      )}
      {totalPages > 1 && (
        <PaginationBar page={page} totalPages={totalPages} total={filtered.length} pageSize={PAGE_SIZE} onPageChange={setPage} />
      )}
    </div>
    </>
  )
}
