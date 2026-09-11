import { useState } from 'react'

interface LostReasonModalProps {
  open: boolean
  leadName: string
  pending: boolean
  onConfirm: (reason: string) => void
  onCancel: () => void
}

/**
 * Se muestra cada vez que una consulta se mueve a "Perdida" — por drag & drop o por el selector
 * accesible del detalle — porque el motivo es obligatorio en ese estado (contrato de la API).
 */
export function LostReasonModal({ open, leadName, pending, onConfirm, onCancel }: LostReasonModalProps) {
  const [reason, setReason] = useState('')

  if (!open) return null

  function handleConfirm() {
    if (!reason.trim()) return
    onConfirm(reason.trim())
    setReason('')
  }

  return (
    <div
      role="presentation"
      onClick={onCancel}
      style={{
        position: 'fixed', inset: 0, background: 'rgba(20,20,16,.4)',
        display: 'grid', placeItems: 'center', zIndex: 320, padding: 16,
      }}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Motivo de pérdida"
        onClick={(e) => e.stopPropagation()}
        className="card"
        style={{ width: '100%', maxWidth: 400, padding: 20, display: 'flex', flexDirection: 'column', gap: 12 }}
      >
        <div>
          <div style={{ fontSize: 16, fontWeight: 600, letterSpacing: '-.01em' }}>Marcar como perdida</div>
          <div style={{ fontSize: 'var(--fs-sm)', color: 'var(--muted)', marginTop: 4 }}>
            Contanos por qué se perdió la consulta de {leadName}.
          </div>
        </div>
        <div>
          <label className="label" htmlFor="lost-reason">Motivo *</label>
          <textarea
            id="lost-reason"
            className="input"
            style={{ height: 84, resize: 'vertical', paddingTop: 8 }}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            autoFocus
            placeholder="No respondió, eligió otra propiedad, precio…"
          />
        </div>
        <div className="row" style={{ justifyContent: 'flex-end', gap: 8 }}>
          <button className="btn btn--sm" onClick={onCancel}>Cancelar</button>
          <button
            className="btn btn--sm btn--danger"
            onClick={handleConfirm}
            disabled={!reason.trim() || pending}
          >
            {pending ? 'Guardando…' : 'Marcar como perdida'}
          </button>
        </div>
      </div>
    </div>
  )
}
