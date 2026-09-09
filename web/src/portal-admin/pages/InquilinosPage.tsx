import { useState } from 'react'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { useAppTenants, useCreateAppTenant, useUpdateAppTenant, useDeleteAppTenant, useInviteTenant } from '@/features/apptenants/hooks/useAppTenants'
import type { AppTenantDto } from '@/features/apptenants/types/apptenant.types'
import { IcUsers, IcPlus, IcEdit, IcMail, IcArchive } from '@/shared/components/ui/Icons'
import { PaginationBar } from '@/shared/components/ui/PaginationBar'
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog'
import { QueryError } from '@/shared/components/ui/QueryError'

const PAGE_SIZE = 20

type FormState = {
  firstName: string; lastName: string; dni: string; email: string; phone: string
}

const EMPTY_FORM: FormState = { firstName: '', lastName: '', dni: '', email: '', phone: '' }

export default function InquilinosPage() {
  const { data: tenants, isLoading, error } = useAppTenants()
  const create = useCreateAppTenant()
  const update = useUpdateAppTenant()
  const remove = useDeleteAppTenant()
  const invite = useInviteTenant()

  const [showForm, setShowForm] = useState(false)
  const [editing, setEditing] = useState<AppTenantDto | null>(null)
  const [form, setForm] = useState<FormState>(EMPTY_FORM)
  const [search, setSearch] = useState('')
  const [err, setErr] = useState('')
  const [inviteResult, setInviteResult] = useState<{ name: string; pass: string } | null>(null)
  const [page, setPage] = useState(0)
  const [confirmArchive, setConfirmArchive] = useState<AppTenantDto | null>(null)
  const [actionErr, setActionErr] = useState<string | null>(null)

  const filtered = (tenants ?? []).filter(t => {
    const q = search.toLowerCase()
    return (
      t.firstName.toLowerCase().includes(q) ||
      t.lastName.toLowerCase().includes(q) ||
      t.dni.includes(q)
    )
  })

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE))
  const paginated = filtered.slice(page * PAGE_SIZE, (page + 1) * PAGE_SIZE)

  function openCreate() {
    setEditing(null)
    setForm(EMPTY_FORM)
    setErr('')
    setShowForm(true)
  }

  function openEdit(t: AppTenantDto) {
    setEditing(t)
    setForm({
      firstName: t.firstName, lastName: t.lastName, dni: t.dni,
      email: t.email ?? '', phone: t.phone ?? '',
    })
    setErr('')
    setShowForm(true)
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setErr('')
    const payload = {
      firstName: form.firstName.trim(),
      lastName: form.lastName.trim(),
      dni: form.dni.trim(),
      email: form.email.trim() || null,
      phone: form.phone.trim() || null,
    }
    try {
      if (editing) {
        await update.mutateAsync({ id: editing.id, req: { ...payload, isActive: editing.isActive } })
      } else {
        await create.mutateAsync(payload)
      }
      setShowForm(false)
    } catch {
      setErr('Error al guardar. El DNI puede estar duplicado o los datos son inválidos.')
    }
  }

  async function handleInvite(t: AppTenantDto) {
    setActionErr(null)
    if (!t.email) {
      setActionErr('Este inquilino no tiene email. Agregá uno primero para invitarlo.')
      return
    }
    try {
      const result = await invite.mutateAsync(t.id)
      setInviteResult({ name: `${result.tenant.firstName} ${result.tenant.lastName}`, pass: result.tempPassword })
    } catch {
      setActionErr('No se pudo invitar. El inquilino puede ya tener acceso al portal.')
    }
  }

  async function doArchive() {
    const t = confirmArchive
    setConfirmArchive(null)
    if (t) await remove.mutateAsync(t.id)
  }

  return (
    <>
      <AdminTopbar
        crumbs={['Inquilinos']}
        right={<button className="btn btn--sm btn--primary" onClick={openCreate}><IcPlus size={12} /> Nuevo inquilino</button>}
      />
      <ConfirmDialog
        open={!!confirmArchive}
        title="Archivar inquilino"
        description={confirmArchive ? `${confirmArchive.firstName} ${confirmArchive.lastName} quedará archivado y no aparecerá en los listados activos.` : ''}
        confirmLabel="Archivar"
        destructive
        onConfirm={doArchive}
        onCancel={() => setConfirmArchive(null)}
      />
    <div className="page">
      <div className="page-h">
        <div>
          <h1>Inquilinos</h1>
          <div className="lead">Personas con contrato en la organización</div>
        </div>
      </div>

      {actionErr && (
        <div role="alert" className="card" style={{ padding: '10px 14px', color: 'var(--danger)', fontSize: 'var(--fs-sm)' }}>
          {actionErr}
        </div>
      )}

      <input
        className="input input--sm"
        style={{ width: 280 }}
        placeholder="Buscar por nombre o DNI…"
        value={search}
        onChange={e => { setSearch(e.target.value); setPage(0) }}
      />

      {inviteResult && (
        <div className="card" style={{ padding: 16, display: 'flex', flexDirection: 'column', gap: 8, borderColor: 'var(--brand-100)', background: 'var(--brand-50)' }}>
          <div style={{ fontWeight: 600 }}>Invitación generada para {inviteResult.name}</div>
          <div className="row" style={{ gap: 8, fontSize: 'var(--fs-sm)' }}>
            <span>Contraseña temporal:</span>
            <code style={{ fontFamily: 'var(--font-mono)', fontWeight: 700 }}>{inviteResult.pass}</code>
            <button
              className="btn btn--sm"
              onClick={() => navigator.clipboard?.writeText(inviteResult.pass)}
              aria-label="Copiar contraseña temporal"
            >
              Copiar
            </button>
          </div>
          <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>Compartí esta contraseña de forma segura. No se volverá a mostrar. El inquilino podrá cambiarla al ingresar.</div>
          <div>
            <button className="btn btn--sm" onClick={() => setInviteResult(null)}>Cerrar</button>
          </div>
        </div>
      )}

      {error && <QueryError message="Error al cargar inquilinos." />}

      {showForm && (
        <div className="card" style={{ padding: 16, display: 'flex', flexDirection: 'column', gap: 14, maxWidth: 640 }}>
          <h2 style={{ fontWeight: 600, margin: 0 }}>{editing ? 'Editar inquilino' : 'Nuevo inquilino'}</h2>
          {err && <div role="alert" style={{ fontSize: 'var(--fs-sm)', color: 'var(--danger)' }}>{err}</div>}
          <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            <div className="grid-2">
              <div>
                <label className="label">Nombre *</label>
                <input className="input" value={form.firstName} onChange={e => setForm(f => ({ ...f, firstName: e.target.value }))} required />
              </div>
              <div>
                <label className="label">Apellido *</label>
                <input className="input" value={form.lastName} onChange={e => setForm(f => ({ ...f, lastName: e.target.value }))} required />
              </div>
            </div>
            <div>
              <label className="label">DNI *</label>
              <input
                className="input"
                value={form.dni}
                onChange={e => setForm(f => ({ ...f, dni: e.target.value }))}
                pattern="^\d{7,8}$"
                title="7 u 8 dígitos numéricos"
                required
              />
            </div>
            <div>
              <label className="label">Email</label>
              <input
                className="input"
                type="email"
                value={form.email}
                onChange={e => setForm(f => ({ ...f, email: e.target.value }))}
                placeholder="Necesario para acceso al portal"
              />
            </div>
            <div>
              <label className="label">Teléfono</label>
              <input className="input" value={form.phone} onChange={e => setForm(f => ({ ...f, phone: e.target.value }))} />
            </div>
            <div className="row" style={{ gap: 8, justifyContent: 'flex-end' }}>
              <button type="button" className="btn btn--sm" onClick={() => setShowForm(false)}>Cancelar</button>
              <button type="submit" className="btn btn--sm btn--primary" disabled={create.isPending || update.isPending}>
                {editing ? 'Guardar cambios' : 'Crear inquilino'}
              </button>
            </div>
          </form>
        </div>
      )}

      {isLoading ? (
        <div className="card" style={{ padding: 48, textAlign: 'center', color: 'var(--muted)' }}>Cargando…</div>
      ) : filtered.length === 0 ? (
        <div className="card" style={{ padding: 48, textAlign: 'center', color: 'var(--muted)' }}>
          <IcUsers size={32} style={{ margin: '0 auto 8px', display: 'block' }} />
          No hay inquilinos.
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          {paginated.map(t => (
            <div key={t.id} className="card row" style={{ padding: '12px 14px', gap: 12 }}>
              <div
                className="mono-avatar"
                style={{ background: 'var(--brand-50)', color: 'var(--brand-700)', borderColor: 'var(--brand-100)' }}
              >
                {t.firstName[0]}{t.lastName[0]}
              </div>
              <div style={{ flex: 1, minWidth: 0 }}>
                <div style={{ fontWeight: 500 }}>{t.firstName} {t.lastName}</div>
                <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>
                  DNI {t.dni}
                  {t.email ? ` · ${t.email}` : ' · Sin email'}
                  {t.userId ? ' · Portal activo' : ''}
                </div>
              </div>
              {!t.isActive && (
                <span className="chip chip--warn" style={{ height: 20, fontSize: 10 }}>Archivado</span>
              )}
              {t.isActive && !t.userId && (
                <button className="btn btn--sm" onClick={() => handleInvite(t)} disabled={invite.isPending}>
                  <IcMail size={14} /> Invitar
                </button>
              )}
              <button className="btn btn--ghost btn--sm btn--icon" onClick={() => openEdit(t)} aria-label="Editar inquilino" title="Editar">
                <IcEdit size={14} />
              </button>
              {t.isActive && (
                <button className="btn btn--ghost btn--sm btn--icon" onClick={() => setConfirmArchive(t)} aria-label="Archivar inquilino" title="Archivar">
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
