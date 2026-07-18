import { useState } from 'react'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { useAppTenants, useCreateAppTenant, useUpdateAppTenant, useDeleteAppTenant, useInviteTenant } from '@/features/apptenants/hooks/useAppTenants'
import type { AppTenantDto } from '@/features/apptenants/types/apptenant.types'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import { IcUsers, IcPlus, IcEdit, IcMail, IcArchive } from '@/shared/components/ui/Icons'
import { PaginationBar } from '@/shared/components/ui/PaginationBar'

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
    if (!t.email) {
      alert('Este inquilino no tiene email. Agregá uno primero para invitarlo.')
      return
    }
    try {
      const result = await invite.mutateAsync(t.id)
      setInviteResult({ name: `${result.tenant.firstName} ${result.tenant.lastName}`, pass: result.tempPassword })
    } catch {
      alert('No se pudo invitar. El inquilino puede ya tener acceso al portal.')
    }
  }

  async function handleDelete(id: string) {
    if (!confirm('¿Archivar este inquilino?')) return
    await remove.mutateAsync(id)
  }

  return (
    <>
      <AdminTopbar
        crumbs={['Inquilinos']}
        right={<Button size="sm" onClick={openCreate}><IcPlus size={12} /> Nuevo inquilino</Button>}
      />
    <div className="p-6 space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Inquilinos</h1>
      </div>

      <Input
        placeholder="Buscar por nombre o DNI..."
        value={search}
        onChange={e => { setSearch(e.target.value); setPage(0) }}
        className="max-w-sm"
      />

      {inviteResult && (
        <div className="card p-4 space-y-1" style={{ borderColor: 'var(--brand)', background: 'var(--brand-50)' }}>
          <p className="font-semibold">Invitación generada para {inviteResult.name}</p>
          <p className="text-sm">Contraseña temporal: <code className="font-mono font-bold">{inviteResult.pass}</code></p>
          <p className="text-xs text-muted-foreground">Compartí esta contraseña de forma segura. El inquilino podrá cambiarla al ingresar.</p>
          <Button size="sm" variant="outline" onClick={() => setInviteResult(null)}>Cerrar</Button>
        </div>
      )}

      {isLoading && <p className="text-sm text-muted-foreground">Cargando...</p>}
      {error && <p className="text-sm text-destructive">Error al cargar inquilinos.</p>}

      {showForm && (
        <div className="card p-4 space-y-4 max-w-xl">
          <h2 className="font-semibold">{editing ? 'Editar inquilino' : 'Nuevo inquilino'}</h2>
          {err && <p className="text-sm text-destructive">{err}</p>}
          <form onSubmit={handleSubmit} className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>Nombre *</Label>
                <Input value={form.firstName} onChange={e => setForm(f => ({ ...f, firstName: e.target.value }))} required />
              </div>
              <div>
                <Label>Apellido *</Label>
                <Input value={form.lastName} onChange={e => setForm(f => ({ ...f, lastName: e.target.value }))} required />
              </div>
            </div>
            <div>
              <Label>DNI *</Label>
              <Input
                value={form.dni}
                onChange={e => setForm(f => ({ ...f, dni: e.target.value }))}
                pattern="^\d{7,8}$"
                title="7 u 8 dígitos numéricos"
                required
              />
            </div>
            <div>
              <Label>Email</Label>
              <Input
                type="email"
                value={form.email}
                onChange={e => setForm(f => ({ ...f, email: e.target.value }))}
                placeholder="Necesario para acceso al portal"
              />
            </div>
            <div>
              <Label>Teléfono</Label>
              <Input value={form.phone} onChange={e => setForm(f => ({ ...f, phone: e.target.value }))} />
            </div>
            <div className="flex gap-2 justify-end">
              <Button type="button" variant="outline" onClick={() => setShowForm(false)}>Cancelar</Button>
              <Button type="submit" disabled={create.isPending || update.isPending}>
                {editing ? 'Guardar cambios' : 'Crear inquilino'}
              </Button>
            </div>
          </form>
        </div>
      )}

      <div className="space-y-2">
        {filtered.length === 0 && !isLoading && (
          <div className="text-center py-12 text-muted-foreground">
            <IcUsers size={40} style={{ margin: '0 auto 8px' }} />
            <p>No hay inquilinos.</p>
          </div>
        )}
        {paginated.map(t => (
          <div key={t.id} className="card p-4 flex items-center gap-3">
            <div
              className="mono-avatar"
              style={{ background: 'var(--brand-50)', color: 'var(--brand-700)', borderColor: 'var(--brand-100)' }}
            >
              {t.firstName[0]}{t.lastName[0]}
            </div>
            <div style={{ flex: 1, minWidth: 0 }}>
              <div className="font-medium">{t.firstName} {t.lastName}</div>
              <div className="text-sm text-muted-foreground">
                DNI {t.dni}
                {t.email ? ` · ${t.email}` : ' · Sin email'}
                {t.userId ? ' · Portal activo' : ''}
              </div>
            </div>
            {!t.isActive && (
              <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted-foreground)' }}>Archivado</span>
            )}
            {t.isActive && !t.userId && (
              <Button size="sm" variant="outline" onClick={() => handleInvite(t)} disabled={invite.isPending}>
                <IcMail size={14} /> Invitar
              </Button>
            )}
            <Button size="sm" variant="ghost" onClick={() => openEdit(t)} aria-label="Editar inquilino" title="Editar">
              <IcEdit size={14} />
            </Button>
            {t.isActive && (
              <Button size="sm" variant="ghost" onClick={() => handleDelete(t.id)} aria-label="Archivar inquilino" title="Archivar">
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
