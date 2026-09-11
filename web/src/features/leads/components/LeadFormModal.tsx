import { useState } from 'react'
import { useCreateLead } from '../hooks/useLeads'
import { ListingPicker } from './ListingPicker'

interface LeadFormModalProps {
  open: boolean
  onClose: () => void
}

/** "Nueva consulta" — carga manual de una consulta desde el panel (Source = Manual). */
export function LeadFormModal({ open, onClose }: LeadFormModalProps) {
  const create = useCreateLead()
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [message, setMessage] = useState('')
  const [listingId, setListingId] = useState<string | undefined>(undefined)
  const [err, setErr] = useState<string | null>(null)

  if (!open) return null

  function reset() {
    setName(''); setEmail(''); setPhone(''); setMessage(''); setListingId(undefined); setErr(null)
  }

  function handleClose() {
    reset()
    onClose()
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setErr(null)
    if (!name.trim() || !message.trim() || (!email.trim() && !phone.trim())) {
      setErr('Nombre, mensaje y al menos un email o teléfono son obligatorios.')
      return
    }
    try {
      await create.mutateAsync({
        name: name.trim(),
        email: email.trim() || undefined,
        phone: phone.trim() || undefined,
        message: message.trim(),
        listingId,
      })
      handleClose()
    } catch {
      setErr('No pudimos guardar la consulta. Intentá de nuevo.')
    }
  }

  return (
    <div
      role="presentation"
      onClick={handleClose}
      style={{ position: 'fixed', inset: 0, background: 'rgba(20,20,16,.4)', display: 'grid', placeItems: 'center', zIndex: 300, padding: 16 }}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Nueva consulta"
        onClick={(e) => e.stopPropagation()}
        className="card"
        style={{ width: '100%', maxWidth: 460, maxHeight: '90vh', overflowY: 'auto' }}
      >
        <div className="card-h">
          <h3>Nueva consulta</h3>
          <button className="btn btn--ghost btn--icon btn--sm" onClick={handleClose} aria-label="Cerrar">×</button>
        </div>
        <form onSubmit={handleSubmit} className="card-b" style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          <div>
            <label className="label" htmlFor="lead-name">Nombre *</label>
            <input id="lead-name" className="input" value={name} onChange={(e) => setName(e.target.value)} required />
          </div>
          <div className="grid-2">
            <div>
              <label className="label" htmlFor="lead-email">Email</label>
              <input id="lead-email" type="email" className="input" value={email} onChange={(e) => setEmail(e.target.value)} />
            </div>
            <div>
              <label className="label" htmlFor="lead-phone">Teléfono</label>
              <input id="lead-phone" type="tel" className="input" value={phone} onChange={(e) => setPhone(e.target.value)} />
            </div>
          </div>
          <div>
            <label className="label" htmlFor="lead-message">Mensaje *</label>
            <textarea
              id="lead-message"
              className="input"
              style={{ height: 90, resize: 'vertical', paddingTop: 8 }}
              value={message}
              onChange={(e) => setMessage(e.target.value)}
              required
            />
          </div>
          <div>
            <label className="label">Publicación (opcional)</label>
            <ListingPicker value={listingId} onChange={setListingId} />
          </div>
          {err && <div role="alert" style={{ color: 'var(--danger)', fontSize: 'var(--fs-sm)' }}>{err}</div>}
          <div className="row" style={{ justifyContent: 'flex-end', gap: 8 }}>
            <button type="button" className="btn btn--sm" onClick={handleClose}>Cancelar</button>
            <button type="submit" className="btn btn--sm btn--primary" disabled={create.isPending}>
              {create.isPending ? 'Guardando…' : 'Crear consulta'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
