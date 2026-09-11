import { useState } from 'react'
import { isAxiosError } from 'axios'
import {
  useListingsByProperty, useCreateListing, useUpdateListing, useDeleteListing,
} from '@/features/listings/hooks/useListings'
import type {
  ListingCurrency, ListingDto, ListingOperationType, ListingStatus,
} from '@/features/listings/types/listing.types'
import type { PropertyDto } from '../types/property.types'
import { IcEdit, IcPlus, IcTag } from '@/shared/components/ui/Icons'
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog'
import { QueryError } from '@/shared/components/ui/QueryError'
import { formatMoney } from '@/shared/lib/formatters'

const OPERATION_TYPES: { value: ListingOperationType; label: string }[] = [
  { value: 'Rent', label: 'Alquiler' },
  { value: 'TemporaryRent', label: 'Alquiler temporario' },
  { value: 'Sale', label: 'Venta' },
]

const LISTING_STATUSES: { value: ListingStatus; label: string }[] = [
  { value: 'Draft', label: 'Borrador' },
  { value: 'Published', label: 'Publicada' },
  { value: 'Reserved', label: 'Reservada' },
  { value: 'Sold', label: 'Vendida' },
  { value: 'Rented', label: 'Alquilada' },
  { value: 'Paused', label: 'Pausada' },
]

type ListingFormState = {
  operationType: ListingOperationType
  price: string
  currency: ListingCurrency
  expenses: string
  title: string
  isFeatured: boolean
  status: ListingStatus
}

const EMPTY_LISTING_FORM: ListingFormState = {
  operationType: 'Rent', price: '', currency: 'ARS', expenses: '', title: '', isFeatured: false, status: 'Draft',
}

const CONFLICT_MESSAGE = 'Ya hay una publicación activa para esa operación.'

export function PropertyListingsTab({ property }: { property: PropertyDto }) {
  const { data: listings, isLoading, error, refetch } = useListingsByProperty(property.id)
  const create = useCreateListing()
  const update = useUpdateListing()
  const remove = useDeleteListing()

  const [showForm, setShowForm] = useState(false)
  const [editing, setEditing] = useState<ListingDto | null>(null)
  const [form, setForm] = useState<ListingFormState>(EMPTY_LISTING_FORM)
  const [err, setErr] = useState('')
  const [confirmDelete, setConfirmDelete] = useState<ListingDto | null>(null)

  function openCreate() {
    setEditing(null)
    setForm(EMPTY_LISTING_FORM)
    setErr('')
    setShowForm(true)
  }

  function openEdit(l: ListingDto) {
    setEditing(l)
    setForm({
      operationType: l.operationType,
      price: String(l.price),
      currency: l.currency,
      expenses: l.expenses?.toString() ?? '',
      title: l.title,
      isFeatured: l.isFeatured,
      status: l.status,
    })
    setErr('')
    setShowForm(true)
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setErr('')
    const price = parseFloat(form.price)
    if (!form.title.trim() || Number.isNaN(price)) {
      setErr('Completá el título y un precio válido.')
      return
    }
    const expenses = form.expenses ? parseFloat(form.expenses) : null
    try {
      if (editing) {
        await update.mutateAsync({
          id: editing.id,
          req: {
            operationType: form.operationType, price, currency: form.currency, expenses,
            title: form.title.trim(), isFeatured: form.isFeatured, status: form.status,
          },
        })
      } else {
        await create.mutateAsync({
          propertyId: property.id, operationType: form.operationType, price, currency: form.currency,
          expenses, title: form.title.trim(), isFeatured: form.isFeatured,
        })
      }
      setShowForm(false)
    } catch (submitError: unknown) {
      if (isAxiosError(submitError) && submitError.response?.status === 409) {
        setErr(CONFLICT_MESSAGE)
      } else {
        setErr('Error al guardar. Verificá los datos e intentá de nuevo.')
      }
    }
  }

  async function handleStatusChange(l: ListingDto, status: ListingStatus) {
    await update.mutateAsync({
      id: l.id,
      req: {
        operationType: l.operationType, price: l.price, currency: l.currency,
        expenses: l.expenses, title: l.title, isFeatured: l.isFeatured, status,
      },
    })
  }

  return (
    <>
      <div className="between" style={{ marginBottom: 12 }}>
        <div style={{ fontSize: 'var(--fs-sm)', fontWeight: 500 }}>
          {listings?.length ?? 0} publicación{(listings?.length ?? 0) !== 1 ? 'es' : ''}
        </div>
        <button className="btn btn--sm btn--primary" onClick={openCreate}>
          <IcPlus size={12} /> Nueva publicación
        </button>
      </div>

      {error && <QueryError message="No pudimos cargar las publicaciones." onRetry={() => refetch()} />}

      {showForm && (
        <div className="card" style={{ padding: 14, display: 'flex', flexDirection: 'column', gap: 10, marginBottom: 12 }}>
          <h3 style={{ margin: 0, fontSize: 'var(--fs-sm)', fontWeight: 600 }}>
            {editing ? 'Editar publicación' : 'Nueva publicación'}
          </h3>
          {err && <div role="alert" style={{ fontSize: 'var(--fs-xs)', color: 'var(--danger)' }}>{err}</div>}
          <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
            <div>
              <label className="label" htmlFor="listing-title">Título *</label>
              <input
                id="listing-title" className="input" value={form.title}
                onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} required
              />
            </div>
            <div className="grid-2">
              <div>
                <label className="label" htmlFor="listing-op">Operación *</label>
                <select
                  id="listing-op" className="select" value={form.operationType}
                  onChange={(e) => setForm((f) => ({ ...f, operationType: e.target.value as ListingOperationType }))}
                >
                  {OPERATION_TYPES.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
                </select>
              </div>
              <div>
                <label className="label" htmlFor="listing-status">Estado</label>
                <select
                  id="listing-status" className="select" value={form.status}
                  onChange={(e) => setForm((f) => ({ ...f, status: e.target.value as ListingStatus }))}
                >
                  {LISTING_STATUSES.map((s) => <option key={s.value} value={s.value}>{s.label}</option>)}
                </select>
              </div>
            </div>
            <div className="grid-2">
              <div>
                <label className="label" htmlFor="listing-price">Precio *</label>
                <input
                  id="listing-price" className="input" type="number" min="0" step="0.01" value={form.price}
                  onChange={(e) => setForm((f) => ({ ...f, price: e.target.value }))} required
                />
              </div>
              <div>
                <label className="label" htmlFor="listing-currency">Moneda *</label>
                <select
                  id="listing-currency" className="select" value={form.currency}
                  onChange={(e) => setForm((f) => ({ ...f, currency: e.target.value as ListingCurrency }))}
                >
                  <option value="ARS">ARS</option>
                  <option value="USD">USD</option>
                </select>
              </div>
            </div>
            <div className="grid-2">
              <div>
                <label className="label" htmlFor="listing-expenses">Expensas</label>
                <input
                  id="listing-expenses" className="input" type="number" min="0" step="0.01" value={form.expenses}
                  onChange={(e) => setForm((f) => ({ ...f, expenses: e.target.value }))}
                />
              </div>
              <div style={{ display: 'flex', alignItems: 'flex-end', paddingBottom: 6 }}>
                <label className="row" style={{ gap: 6, fontSize: 'var(--fs-sm)', cursor: 'pointer' }}>
                  <input
                    type="checkbox" checked={form.isFeatured}
                    onChange={(e) => setForm((f) => ({ ...f, isFeatured: e.target.checked }))}
                  />
                  Destacada
                </label>
              </div>
            </div>
            <div className="row" style={{ gap: 8, justifyContent: 'flex-end' }}>
              <button type="button" className="btn btn--sm" onClick={() => setShowForm(false)}>Cancelar</button>
              <button type="submit" className="btn btn--sm btn--primary" disabled={create.isPending || update.isPending}>
                {editing ? 'Guardar cambios' : 'Crear publicación'}
              </button>
            </div>
          </form>
        </div>
      )}

      {isLoading ? (
        <div style={{ padding: 24, textAlign: 'center', color: 'var(--muted)' }}>Cargando…</div>
      ) : !listings || listings.length === 0 ? (
        <div style={{ padding: 32, textAlign: 'center', color: 'var(--muted)' }}>
          <IcTag size={28} style={{ opacity: .3, display: 'block', margin: '0 auto 8px' }} />
          Sin publicaciones para esta propiedad.
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          {listings.map((l) => (
            <div key={l.id} className="card" style={{ padding: '10px 12px', display: 'flex', flexDirection: 'column', gap: 6 }}>
              <div className="between">
                <div className="row" style={{ gap: 8 }}>
                  <b style={{ fontSize: 'var(--fs-sm)' }}>{l.title}</b>
                  {l.isFeatured && <span className="chip chip--info" style={{ height: 18, fontSize: 9 }}>Destacada</span>}
                </div>
                <div className="row" style={{ gap: 4 }}>
                  <button
                    className="btn btn--ghost btn--sm btn--icon" title="Editar publicación"
                    aria-label="Editar publicación" onClick={() => openEdit(l)}
                  >
                    <IcEdit size={13} />
                  </button>
                  <button
                    className="btn btn--ghost btn--sm btn--icon" style={{ color: 'var(--danger)' }}
                    title="Eliminar publicación" aria-label="Eliminar publicación" onClick={() => setConfirmDelete(l)}
                  >
                    ×
                  </button>
                </div>
              </div>
              <div className="row" style={{ gap: 10, fontSize: 'var(--fs-xs)', color: 'var(--muted)', flexWrap: 'wrap' }}>
                <span>{OPERATION_TYPES.find((o) => o.value === l.operationType)?.label ?? l.operationType}</span>
                <span>{formatMoney(l.price, l.currency)}</span>
                {l.expenses != null && <span>+ {formatMoney(l.expenses, l.currency)} exp.</span>}
                <div className="row" style={{ gap: 4 }}>
                  <label className="label" style={{ margin: 0 }} htmlFor={`listing-status-quick-${l.id}`}>Estado</label>
                  <select
                    id={`listing-status-quick-${l.id}`}
                    className="select"
                    style={{ height: 24, fontSize: 11, padding: '0 6px' }}
                    value={l.status}
                    onChange={(e) => handleStatusChange(l, e.target.value as ListingStatus)}
                  >
                    {LISTING_STATUSES.map((s) => <option key={s.value} value={s.value}>{s.label}</option>)}
                  </select>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      <ConfirmDialog
        open={!!confirmDelete}
        title="Eliminar publicación"
        description={confirmDelete ? `"${confirmDelete.title}" se eliminará de forma permanente.` : ''}
        confirmLabel="Eliminar"
        destructive
        onConfirm={() => { if (confirmDelete) remove.mutate(confirmDelete.id); setConfirmDelete(null) }}
        onCancel={() => setConfirmDelete(null)}
      />
    </>
  )
}
