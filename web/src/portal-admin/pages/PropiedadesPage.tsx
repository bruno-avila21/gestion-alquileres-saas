import { useState } from 'react'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { useProperties, useCreateProperty, useUpdateProperty, useDeleteProperty } from '@/features/properties/hooks/useProperties'
import type { PropertyDto, PropertyType } from '@/features/properties/types/property.types'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import { IcBuilding, IcPlus, IcEdit, IcArchive } from '@/shared/components/ui/Icons'
import { PaginationBar } from '@/shared/components/ui/PaginationBar'

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

  async function handleDelete(id: string) {
    if (!confirm('¿Archivar esta propiedad?')) return
    await remove.mutateAsync(id)
  }

  return (
    <>
      <AdminTopbar
        crumbs={['Propiedades']}
        right={<Button size="sm" onClick={openCreate}><IcPlus size={12} /> Nueva propiedad</Button>}
      />
    <div className="p-6 space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Propiedades</h1>
      </div>

      <Input
        placeholder="Buscar por dirección o ciudad..."
        value={search}
        onChange={e => { setSearch(e.target.value); setPage(0) }}
        className="max-w-sm"
      />

      {isLoading && <p className="text-sm text-muted-foreground">Cargando...</p>}
      {error && <p className="text-sm text-destructive">Error al cargar propiedades.</p>}

      {showForm && (
        <div className="card p-4 space-y-4 max-w-xl">
          <h2 className="font-semibold">{editing ? 'Editar propiedad' : 'Nueva propiedad'}</h2>
          {err && <p className="text-sm text-destructive">{err}</p>}
          <form onSubmit={handleSubmit} className="space-y-3">
            <div>
              <Label>Dirección *</Label>
              <Input value={form.address} onChange={e => setForm(f => ({ ...f, address: e.target.value }))} required />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>Ciudad *</Label>
                <Input value={form.city} onChange={e => setForm(f => ({ ...f, city: e.target.value }))} required />
              </div>
              <div>
                <Label>Provincia *</Label>
                <select
                  className="input w-full"
                  value={form.province}
                  onChange={e => setForm(f => ({ ...f, province: e.target.value }))}
                >
                  {PROVINCES.map(p => <option key={p}>{p}</option>)}
                </select>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>Tipo *</Label>
                <select
                  className="input w-full"
                  value={form.propertyType}
                  onChange={e => setForm(f => ({ ...f, propertyType: e.target.value as PropertyType }))}
                >
                  {PROPERTY_TYPES.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
                </select>
              </div>
              <div>
                <Label>Superficie (m²)</Label>
                <Input
                  type="number"
                  min="1"
                  step="0.01"
                  value={form.areaM2}
                  onChange={e => setForm(f => ({ ...f, areaM2: e.target.value }))}
                />
              </div>
            </div>
            <div>
              <Label>Notas</Label>
              <Input value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} />
            </div>
            <div className="flex gap-2 justify-end">
              <Button type="button" variant="outline" onClick={() => setShowForm(false)}>Cancelar</Button>
              <Button type="submit" disabled={create.isPending || update.isPending}>
                {editing ? 'Guardar cambios' : 'Crear propiedad'}
              </Button>
            </div>
          </form>
        </div>
      )}

      <div className="space-y-2">
        {filtered.length === 0 && !isLoading && (
          <div className="text-center py-12 text-muted-foreground">
            <IcBuilding size={40} style={{ margin: '0 auto 8px' }} />
            <p>No hay propiedades.</p>
          </div>
        )}
        {paginated.map(p => (
          <div key={p.id} className="card p-4 flex items-center gap-3">
            <IcBuilding size={20} style={{ color: 'var(--brand)', flexShrink: 0 }} />
            <div style={{ flex: 1, minWidth: 0 }}>
              <div className="font-medium">{p.address}</div>
              <div className="text-sm text-muted-foreground">
                {p.city}, {p.province} · {PROPERTY_TYPES.find(t => t.value === p.propertyType)?.label ?? p.propertyType}
                {p.areaM2 ? ` · ${p.areaM2} m²` : ''}
              </div>
            </div>
            {!p.isActive && (
              <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted-foreground)' }}>Archivada</span>
            )}
            <Button size="sm" variant="ghost" onClick={() => openEdit(p)} aria-label="Editar propiedad" title="Editar">
              <IcEdit size={14} />
            </Button>
            {p.isActive && (
              <Button size="sm" variant="ghost" onClick={() => handleDelete(p.id)} aria-label="Archivar propiedad" title="Archivar">
                <IcArchive size={14} />
              </Button>
            )}
          </div>
        ))}
      </div>
      {totalPages > 1 && (
        <PaginationBar page={page} totalPages={totalPages} total={filtered.length} pageSize={PAGE_SIZE} onPageChange={setPage} />
      )}
    </div>
    </>
  )
}
