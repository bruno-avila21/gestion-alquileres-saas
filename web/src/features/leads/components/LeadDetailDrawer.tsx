import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog'
import { QueryError } from '@/shared/components/ui/QueryError'
import { IcEdit, IcLink, IcMail, IcPhone } from '@/shared/components/ui/Icons'
import { formatDate } from '@/shared/lib/formatters'
import {
  useAddLeadNote, useDeleteLead, useLead, useUpdateLead, useUpdateLeadStatus,
} from '../hooks/useLeads'
import { LEAD_STATUS_LABELS, LEAD_STATUSES } from '../types/lead.types'
import type { LeadDetailDto, LeadStatus } from '../types/lead.types'
import { LostReasonModal } from './LostReasonModal'

interface LeadDetailDrawerProps {
  leadId: string | null
  onClose: () => void
}

interface EditState {
  name: string
  email: string
  phone: string
  message: string
}

function toEditState(lead: LeadDetailDto): EditState {
  return { name: lead.name, email: lead.email ?? '', phone: lead.phone ?? '', message: lead.message }
}

export function LeadDetailDrawer({ leadId, onClose }: LeadDetailDrawerProps) {
  const { data: lead, isLoading, isError, refetch } = useLead(leadId ?? undefined)
  const updateStatus = useUpdateLeadStatus()
  const updateLead = useUpdateLead()
  const addNote = useAddLeadNote()
  const deleteLead = useDeleteLead()

  const [isEditing, setIsEditing] = useState(false)
  const [edit, setEdit] = useState<EditState>({ name: '', email: '', phone: '', message: '' })
  const [editErr, setEditErr] = useState<string | null>(null)
  const [noteText, setNoteText] = useState('')
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [pendingLostStatus, setPendingLostStatus] = useState(false)

  useEffect(() => {
    setIsEditing(false)
    setEditErr(null)
    setNoteText('')
    setConfirmDelete(false)
    setPendingLostStatus(false)
  }, [leadId])

  if (!leadId) return null

  function handleStatusChange(status: LeadStatus) {
    if (!lead || status === lead.status) return
    if (status === 'Lost') {
      setPendingLostStatus(true)
      return
    }
    updateStatus.mutate({ id: lead.id, req: { status } })
  }

  function confirmLost(reason: string) {
    if (!lead) return
    updateStatus.mutate({ id: lead.id, req: { status: 'Lost', lostReason: reason } })
    setPendingLostStatus(false)
  }

  function startEdit() {
    if (!lead) return
    setEdit(toEditState(lead))
    setEditErr(null)
    setIsEditing(true)
  }

  async function handleSaveEdit(e: React.FormEvent) {
    e.preventDefault()
    if (!lead) return
    setEditErr(null)
    if (!edit.name.trim() || !edit.message.trim() || (!edit.email.trim() && !edit.phone.trim())) {
      setEditErr('Nombre, mensaje y al menos un email o teléfono son obligatorios.')
      return
    }
    try {
      await updateLead.mutateAsync({
        id: lead.id,
        req: {
          name: edit.name.trim(),
          email: edit.email.trim() || undefined,
          phone: edit.phone.trim() || undefined,
          message: edit.message.trim(),
        },
      })
      setIsEditing(false)
    } catch {
      setEditErr('No pudimos guardar los cambios. Intentá de nuevo.')
    }
  }

  async function handleAddNote() {
    if (!lead || !noteText.trim()) return
    await addNote.mutateAsync({ id: lead.id, req: { text: noteText.trim() } })
    setNoteText('')
  }

  async function handleDelete() {
    if (!lead) return
    setConfirmDelete(false)
    await deleteLead.mutateAsync(lead.id)
    onClose()
  }

  return (
    <>
      <div className="lead-drawer-overlay" role="presentation" onClick={onClose} />
      <div className="lead-drawer" role="dialog" aria-modal="true" aria-label="Detalle de la consulta">
        <div className="lead-drawer-h">
          <div style={{ minWidth: 0 }}>
            <div className="label">Consulta</div>
            <h2 style={{ margin: '2px 0 0', fontSize: 18, fontWeight: 600, letterSpacing: '-.01em' }}>
              {lead?.name ?? '…'}
            </h2>
          </div>
          <button className="btn btn--ghost btn--icon btn--sm" onClick={onClose} aria-label="Cerrar">×</button>
        </div>

        <div className="lead-drawer-body">
          {isError ? (
            <QueryError onRetry={() => refetch()} message="No pudimos cargar la consulta." />
          ) : isLoading || !lead ? (
            <div className="muted" style={{ textAlign: 'center', padding: 24 }}>Cargando…</div>
          ) : (
            <>
              <div>
                <label className="label" htmlFor="lead-status-select">Estado</label>
                <select
                  id="lead-status-select"
                  className="select"
                  value={lead.status}
                  onChange={(e) => handleStatusChange(e.target.value as LeadStatus)}
                  disabled={updateStatus.isPending}
                >
                  {LEAD_STATUSES.map((s) => (
                    <option key={s} value={s}>{LEAD_STATUS_LABELS[s]}</option>
                  ))}
                </select>
                {lead.status === 'Lost' && lead.lostReason && (
                  <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', marginTop: 6 }}>
                    Motivo: {lead.lostReason}
                  </div>
                )}
              </div>

              <div className="col" style={{ gap: 6 }}>
                <div className="sect-title">Contacto</div>
                {lead.email && (
                  <a className="row" href={`mailto:${lead.email}`} style={{ gap: 8, color: 'var(--brand)' }}>
                    <IcMail size={13} /> {lead.email}
                  </a>
                )}
                {lead.phone && (
                  <a className="row" href={`tel:${lead.phone}`} style={{ gap: 8, color: 'var(--brand)' }}>
                    <IcPhone size={13} /> {lead.phone}
                  </a>
                )}
                {!lead.email && !lead.phone && <span className="muted" style={{ fontSize: 'var(--fs-sm)' }}>Sin datos de contacto</span>}
              </div>

              {lead.propertyId && (
                <Link
                  className="row"
                  style={{ gap: 6, fontSize: 'var(--fs-sm)', color: 'var(--brand)' }}
                  to={`/admin/propiedades?highlight=${lead.propertyId}`}
                >
                  <IcLink size={13} /> {lead.propertyTitle ?? 'Ver ficha de la propiedad'}
                </Link>
              )}

              {!isEditing ? (
                <div className="col" style={{ gap: 6 }}>
                  <div className="between">
                    <div className="sect-title" style={{ margin: 0 }}>Mensaje</div>
                    <button className="btn btn--ghost btn--sm" onClick={startEdit}>
                      <IcEdit size={12} /> Editar
                    </button>
                  </div>
                  <p style={{ fontSize: 'var(--fs-sm)', lineHeight: 1.6, whiteSpace: 'pre-line', margin: 0 }}>{lead.message}</p>
                </div>
              ) : (
                <form onSubmit={handleSaveEdit} className="col" style={{ gap: 10 }}>
                  <div className="sect-title" style={{ margin: 0 }}>Editar datos</div>
                  <div>
                    <label className="label" htmlFor="edit-name">Nombre *</label>
                    <input id="edit-name" className="input" value={edit.name} onChange={(e) => setEdit((f) => ({ ...f, name: e.target.value }))} />
                  </div>
                  <div className="grid-2">
                    <div>
                      <label className="label" htmlFor="edit-email">Email</label>
                      <input id="edit-email" type="email" className="input" value={edit.email} onChange={(e) => setEdit((f) => ({ ...f, email: e.target.value }))} />
                    </div>
                    <div>
                      <label className="label" htmlFor="edit-phone">Teléfono</label>
                      <input id="edit-phone" type="tel" className="input" value={edit.phone} onChange={(e) => setEdit((f) => ({ ...f, phone: e.target.value }))} />
                    </div>
                  </div>
                  <div>
                    <label className="label" htmlFor="edit-message">Mensaje *</label>
                    <textarea
                      id="edit-message"
                      className="input"
                      style={{ height: 84, resize: 'vertical', paddingTop: 8 }}
                      value={edit.message}
                      onChange={(e) => setEdit((f) => ({ ...f, message: e.target.value }))}
                    />
                  </div>
                  {editErr && <div role="alert" style={{ color: 'var(--danger)', fontSize: 'var(--fs-xs)' }}>{editErr}</div>}
                  <div className="row" style={{ justifyContent: 'flex-end', gap: 8 }}>
                    <button type="button" className="btn btn--sm" onClick={() => setIsEditing(false)}>Cancelar</button>
                    <button type="submit" className="btn btn--sm btn--primary" disabled={updateLead.isPending}>
                      {updateLead.isPending ? 'Guardando…' : 'Guardar'}
                    </button>
                  </div>
                </form>
              )}

              <div className="col" style={{ gap: 10 }}>
                <div className="sect-title" style={{ margin: 0 }}>Notas</div>
                {lead.notes.length === 0 ? (
                  <div className="muted" style={{ fontSize: 'var(--fs-xs)' }}>Todavía no hay notas.</div>
                ) : (
                  <div className="col" style={{ gap: 10 }}>
                    {lead.notes.map((note) => (
                      <div key={note.id} style={{ borderLeft: '2px solid var(--hairline-2)', paddingLeft: 10 }}>
                        <div style={{ fontSize: 'var(--fs-sm)', whiteSpace: 'pre-line' }}>{note.text}</div>
                        <div className="muted" style={{ fontSize: 'var(--fs-xs)', marginTop: 2 }}>
                          {note.createdByName} · {formatDate(note.createdAt.split('T')[0])}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
                <textarea
                  className="input"
                  style={{ height: 64, resize: 'vertical', paddingTop: 8 }}
                  placeholder="Agregar una nota…"
                  value={noteText}
                  onChange={(e) => setNoteText(e.target.value)}
                />
                <button
                  className="btn btn--sm btn--primary"
                  style={{ alignSelf: 'flex-start' }}
                  onClick={handleAddNote}
                  disabled={!noteText.trim() || addNote.isPending}
                >
                  {addNote.isPending ? 'Guardando…' : 'Agregar nota'}
                </button>
              </div>
            </>
          )}
        </div>

        {lead && (
          <div className="lead-drawer-footer">
            <button className="btn btn--sm btn--danger" onClick={() => setConfirmDelete(true)}>
              Eliminar consulta
            </button>
          </div>
        )}
      </div>

      <ConfirmDialog
        open={confirmDelete}
        title="Eliminar consulta"
        description="La consulta y sus notas se eliminarán de forma permanente. Esta acción no se puede deshacer."
        confirmLabel="Eliminar"
        destructive
        onConfirm={handleDelete}
        onCancel={() => setConfirmDelete(false)}
      />

      <LostReasonModal
        open={pendingLostStatus}
        leadName={lead?.name ?? ''}
        pending={updateStatus.isPending}
        onConfirm={confirmLost}
        onCancel={() => setPendingLostStatus(false)}
      />
    </>
  )
}
