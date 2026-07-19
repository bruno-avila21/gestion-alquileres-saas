interface ConfirmDialogProps {
  open: boolean
  title: string
  description?: string
  confirmLabel?: string
  cancelLabel?: string
  /** Style the confirm button as a destructive action (red). */
  destructive?: boolean
  onConfirm: () => void
  onCancel: () => void
}

/**
 * Styled confirmation modal (audit M14). Replaces native confirm()/alert() for business actions
 * (rescindir contrato, archivar, eliminar documento): those block the thread, aren't styled, aren't
 * accessible, and don't explain consequences. Render once per page and drive with local state.
 */
export function ConfirmDialog({
  open, title, description, confirmLabel = 'Confirmar', cancelLabel = 'Cancelar',
  destructive = false, onConfirm, onCancel,
}: ConfirmDialogProps) {
  if (!open) return null
  return (
    <div
      role="presentation"
      onClick={onCancel}
      style={{
        position: 'fixed', inset: 0, background: 'rgba(20,20,16,.4)',
        display: 'grid', placeItems: 'center', zIndex: 300, padding: 16,
      }}
    >
      <div
        role="alertdialog"
        aria-modal="true"
        aria-label={title}
        onClick={(e) => e.stopPropagation()}
        className="card"
        style={{ width: '100%', maxWidth: 380, padding: 20, border: 'none', boxShadow: '0 20px 50px rgba(0,0,0,.2)' }}
      >
        <div style={{ fontSize: 16, fontWeight: 600, letterSpacing: '-.01em' }}>{title}</div>
        {description && (
          <div style={{ fontSize: 'var(--fs-sm)', color: 'var(--muted)', marginTop: 8, lineHeight: 1.5 }}>
            {description}
          </div>
        )}
        <div className="row" style={{ marginTop: 18, gap: 8, justifyContent: 'flex-end' }}>
          <button className="btn btn--sm" onClick={onCancel}>{cancelLabel}</button>
          <button
            className={`btn btn--sm ${destructive ? 'btn--danger' : 'btn--primary'}`}
            onClick={onConfirm}
            autoFocus
          >
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  )
}
