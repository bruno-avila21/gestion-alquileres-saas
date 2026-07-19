import { useState } from 'react'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { useProperties, useCreateProperty, useUpdateProperty, useDeleteProperty } from '@/features/properties/hooks/useProperties'
import type { PropertyDto, PropertyType } from '@/features/properties/types/property.types'
import { IcBuilding, IcPlus, IcEdit, IcArchive } from '@/shared/components/ui/Icons'
import { PaginationBar } from '@/shared/components/ui/PaginationBar'
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog'
import { QueryError } from '@/shared/components/ui/QueryError'

const PAGE_SIZE = 20

const PROPERTY_TYPES: { value: PropertyType; label: string }[] = [
  { value: 'Apartment', label: 'Departamento' },
  { value: 'House', label: 'Casa' },
  { value: 'Commercial', label: 'Local comercial' },
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

type FormState = {
  address: string; city: string; province: string
  propertyType: PropertyType; areaM2: string; notes: string
}

const EMPTY_FORM: FormState = {
  address: '', city: '', province: 'Buenos Aires',
  propertyType: 'Apartment', areaM2: '', notes: '',
}

export default function PropiedadesPage() {
  const { data: properties, isLoading, error } = useProperties()
  const create = useCreateProperty()
  const update = useUpdateProperty()
  const remove = useDeleteProperty()

  const [showForm, setShowForm] = useState(false)
  const [editing, setEditing] = useState<PropertyDto | null>(null)
  const [form, setForm] = useState<FormState>(EMPTY_FORM)
  const [search, setSearch] = useState('')
  const [err, setErr] = useState('')
  const [page, setPage] = useState(0)
  const [confirmArchive, setConfirmArchive] = useState<PropertyDto | null>(null)

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
    setShowForm(true)
  }

  function openEdit(p: PropertyDto) {
    setEditing(p)
    setForm({
      address: p.address, city: p.city, province: p.province,
      propertyType: p.propertyType, areaM2: p.areaM2?.toString() ?? '', notes: p.notes ?? '',
    })
    setErr('')
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
      areaM2: form.areaM2 ? parseFloat(form.areaM2) : null,
      notes: form.notes.trim() || null,
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
              <label className="label">Dirección *</label>
              <input className="input" value={form.address} onChange={e => setForm(f => ({ ...f, address: e.target.value }))} required />
            </div>
            <div className="grid-2">
              <div>
                <label className="label">Ciudad *</label>
                <input className="input" value={form.city} onChange={e => setForm(f => ({ ...f, city: e.target.value }))} required />
              </div>
              <div>
                <label className="label">Provincia *</label>
                <select
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
                <label className="label">Tipo *</label>
                <select
                  className="select"
                  value={form.propertyType}
                  onChange={e => setForm(f => ({ ...f, propertyType: e.target.value as PropertyType }))}
                >
                  {PROPERTY_TYPES.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
                </select>
              </div>
              <div>
                <label className="label">Superficie (m²)</label>
                <input
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
              <label className="label">Notas</label>
              <input className="input" value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} />
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
            <div key={p.id} className="card row" style={{ padding: '12px 14px', gap: 12 }}>
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
              <button className="btn btn--ghost btn--sm btn--icon" onClick={() => openEdit(p)} aria-label="Editar propiedad" title="Editar">
                <IcEdit size={14} />
              </button>
              {p.isActive && (
                <button className="btn btn--ghost btn--sm btn--icon" onClick={() => setConfirmArchive(p)} aria-label="Archivar propiedad" title="Archivar">
                  <IcArchive size={14} />
                </button>
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
