import { useState } from 'react'
import { useNavigate } from 'react-router'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { useContracts, useCreateContract, useTerminateContract } from '@/features/contracts/hooks/useContracts'
import { useProperties } from '@/features/properties/hooks/useProperties'
import { useAppTenants } from '@/features/apptenants/hooks/useAppTenants'
import type { AdjustmentFrequency, AdjustmentType, ContractCurrency, ContractDto, ContractStatus, CreateContractRequest } from '@/features/contracts/types/contract.types'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import {
  IcPlus, IcSearch, IcChevDown, IcDownload, IcChev, IcDoc,
} from '@/shared/components/ui/Icons'
import { formatARS, formatDateShort } from '@/shared/lib/formatters'
import { PaginationBar } from '@/shared/components/ui/PaginationBar'
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog'

const PAGE_SIZE = 20

type FormState = {
  propertyId: string
  appTenantId: string
  startDate: string
  endDate: string
  monthlyRent: string
  currency: ContractCurrency
  adjustmentType: AdjustmentType
  adjustmentFrequency: AdjustmentFrequency
  dayOfMonth: string
  depositAmount: string
  notes: string
}

const EMPTY_FORM: FormState = {
  propertyId: '', appTenantId: '',
  startDate: '', endDate: '',
  monthlyRent: '', currency: 'ARS',
  adjustmentType: 'ICL', adjustmentFrequency: 'Quarterly',
  dayOfMonth: '1', depositAmount: '', notes: '',
}

const STATUS_LABELS: Record<ContractStatus, { cls: string; lbl: string }> = {
  Active: { cls: 'chip--ok', lbl: 'Vigente' },
  Expired: { cls: 'chip--warn', lbl: 'Vencido' },
  Terminated: { cls: 'chip--danger', lbl: 'Rescindido' },
}

const ADJ_LABELS: Record<AdjustmentType, string> = { ICL: 'ICL', IPC: 'IPC', Manual: 'Manual' }

export default function ContratosPage() {
  const navigate = useNavigate()
  const { data: contracts, isLoading, error } = useContracts()
  const { data: properties } = useProperties()
  const { data: tenants } = useAppTenants()
  const create = useCreateContract()
  const terminate = useTerminateContract()

  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState<FormState>(EMPTY_FORM)
  const [search, setSearch] = useState('')
  const [activeStatus, setActiveStatus] = useState<ContractStatus | 'all'>('all')
  const [formErr, setFormErr] = useState('')
  const [page, setPage] = useState(0)
  const [confirmTerminate, setConfirmTerminate] = useState<ContractDto | null>(null)
  const [actionErr, setActionErr] = useState<string | null>(null)

  const filtered = (contracts ?? []).filter(c => {
    const matchStatus = activeStatus === 'all' || c.status === activeStatus
    const q = search.toLowerCase()
    const matchSearch = !q || c.appTenantFullName.toLowerCase().includes(q) || c.propertyAddress.toLowerCase().includes(q)
    return matchStatus && matchSearch
  })

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE))
  const paginated = filtered.slice(page * PAGE_SIZE, (page + 1) * PAGE_SIZE)

  function handleSearch(val: string) { setSearch(val); setPage(0) }
  function handleStatusChange(status: ContractStatus | 'all') { setActiveStatus(status); setPage(0) }

  const counts = {
    all: contracts?.length ?? 0,
    Active: contracts?.filter(c => c.status === 'Active').length ?? 0,
    Expired: contracts?.filter(c => c.status === 'Expired').length ?? 0,
    Terminated: contracts?.filter(c => c.status === 'Terminated').length ?? 0,
  }

  function openCreate() {
    setForm(EMPTY_FORM)
    setFormErr('')
    setShowForm(true)
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setFormErr('')
    const payload: CreateContractRequest = {
      propertyId: form.propertyId,
      appTenantId: form.appTenantId,
      startDate: form.startDate,
      endDate: form.endDate,
      monthlyRent: parseFloat(form.monthlyRent),
      currency: form.currency,
      adjustmentType: form.adjustmentType,
      adjustmentFrequency: form.adjustmentFrequency,
      dayOfMonth: parseInt(form.dayOfMonth, 10),
      depositAmount: form.depositAmount ? parseFloat(form.depositAmount) : null,
      notes: form.notes.trim() || null,
    }
    try {
      await create.mutateAsync(payload)
      setShowForm(false)
    } catch {
      setFormErr('Error al crear el contrato. Verificá los datos seleccionados.')
    }
  }

  function handleTerminate(c: ContractDto) {
    setActionErr(null)
    setConfirmTerminate(c)
  }

  async function doTerminate() {
    const c = confirmTerminate
    setConfirmTerminate(null)
    if (!c) return
    try {
      await terminate.mutateAsync({ id: c.id, req: { notes: 'Rescisión manual' } })
    } catch {
      setActionErr('No se pudo rescindir. El contrato puede ya estar rescindido.')
    }
  }

  return (
    <>
      <AdminTopbar
        crumbs={['Contratos']}
        right={
          <Button size="sm" onClick={openCreate}>
            <IcPlus size={12} /> Nuevo contrato
          </Button>
        }
      />
      <ConfirmDialog
        open={!!confirmTerminate}
        title="Rescindir contrato"
        description={confirmTerminate ? `El contrato de ${confirmTerminate.appTenantFullName} quedará rescindido. Esta acción no se puede deshacer.` : ''}
        confirmLabel="Rescindir"
        destructive
        onConfirm={doTerminate}
        onCancel={() => setConfirmTerminate(null)}
      />
      <div className="page">
        {actionErr && (
          <div role="alert" className="card" style={{ padding: '10px 14px', color: 'var(--danger)', fontSize: 'var(--fs-sm)' }}>
            {actionErr}
          </div>
        )}
        <div className="page-h">
          <div>
            <h1>Contratos</h1>
            <div className="lead">{counts.Active} vigentes</div>
          </div>
          <button className="btn btn--sm">
            <IcDownload size={12} /> Exportar
          </button>
        </div>

        {/* Form */}
        {showForm && (
          <div className="card p-4 space-y-4 max-w-2xl">
            <h2 className="font-semibold">Nuevo contrato</h2>
            {formErr && <p className="text-sm text-destructive">{formErr}</p>}
            <form onSubmit={handleSubmit} className="space-y-3">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <Label>Propiedad *</Label>
                  <select
                    className="input w-full"
                    value={form.propertyId}
                    onChange={e => setForm(f => ({ ...f, propertyId: e.target.value }))}
                    required
                  >
                    <option value="">Seleccioná una propiedad</option>
                    {(properties ?? []).filter(p => p.isActive).map(p => (
                      <option key={p.id} value={p.id}>{p.address} · {p.city}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <Label>Inquilino *</Label>
                  <select
                    className="input w-full"
                    value={form.appTenantId}
                    onChange={e => setForm(f => ({ ...f, appTenantId: e.target.value }))}
                    required
                  >
                    <option value="">Seleccioná un inquilino</option>
                    {(tenants ?? []).filter(t => t.isActive).map(t => (
                      <option key={t.id} value={t.id}>{t.firstName} {t.lastName} · DNI {t.dni}</option>
                    ))}
                  </select>
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <Label>Inicio *</Label>
                  <Input
                    type="date"
                    value={form.startDate}
                    onChange={e => setForm(f => ({ ...f, startDate: e.target.value }))}
                    required
                  />
                </div>
                <div>
                  <Label>Fin *</Label>
                  <Input
                    type="date"
                    value={form.endDate}
                    onChange={e => setForm(f => ({ ...f, endDate: e.target.value }))}
                    required
                  />
                </div>
              </div>
              <div className="grid grid-cols-3 gap-3">
                <div>
                  <Label>Alquiler mensual *</Label>
                  <Input
                    type="number"
                    min="1"
                    step="0.01"
                    value={form.monthlyRent}
                    onChange={e => setForm(f => ({ ...f, monthlyRent: e.target.value }))}
                    required
                  />
                </div>
                <div>
                  <Label>Moneda *</Label>
                  <select
                    className="input w-full"
                    value={form.currency}
                    onChange={e => setForm(f => ({ ...f, currency: e.target.value as ContractCurrency }))}
                  >
                    <option value="ARS">ARS</option>
                    <option value="USD">USD</option>
                  </select>
                </div>
                <div>
                  <Label>Depósito</Label>
                  <Input
                    type="number"
                    min="0"
                    step="0.01"
                    value={form.depositAmount}
                    onChange={e => setForm(f => ({ ...f, depositAmount: e.target.value }))}
                  />
                </div>
              </div>
              <div className="grid grid-cols-3 gap-3">
                <div>
                  <Label>Índice ajuste *</Label>
                  <select
                    className="input w-full"
                    value={form.adjustmentType}
                    onChange={e => setForm(f => ({ ...f, adjustmentType: e.target.value as AdjustmentType }))}
                  >
                    <option value="ICL">ICL</option>
                    <option value="IPC">IPC</option>
                    <option value="Manual">Manual</option>
                  </select>
                </div>
                <div>
                  <Label>Frecuencia *</Label>
                  <select
                    className="input w-full"
                    value={form.adjustmentFrequency}
                    onChange={e => setForm(f => ({ ...f, adjustmentFrequency: e.target.value as AdjustmentFrequency }))}
                  >
                    <option value="Monthly">Mensual</option>
                    <option value="Quarterly">Trimestral</option>
                    <option value="Annual">Anual</option>
                  </select>
                </div>
                <div>
                  <Label>Día de cobro *</Label>
                  <Input
                    type="number"
                    min="1"
                    max="28"
                    value={form.dayOfMonth}
                    onChange={e => setForm(f => ({ ...f, dayOfMonth: e.target.value }))}
                    required
                  />
                </div>
              </div>
              <div>
                <Label>Notas</Label>
                <Input value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} />
              </div>
              <div className="flex gap-2 justify-end">
                <Button type="button" variant="outline" onClick={() => setShowForm(false)}>Cancelar</Button>
                <Button type="submit" disabled={create.isPending}>Crear contrato</Button>
              </div>
            </form>
          </div>
        )}

        {/* Filtros */}
        <div className="card card-b" style={{ display: 'flex', gap: 10, alignItems: 'center', padding: '12px 14px' }}>
          <div
            className="row"
            style={{ background: 'var(--surface-3)', borderRadius: 6, padding: '0 10px', height: 32, flex: 1 }}
          >
            <IcSearch size={14} />
            <input
              style={{
                border: 'none', outline: 'none', background: 'transparent',
                height: '100%', flex: 1, fontSize: 'var(--fs-sm)', fontFamily: 'inherit',
              }}
              placeholder="Buscar por inquilino o dirección…"
              value={search}
              onChange={e => handleSearch(e.target.value)}
            />
          </div>
          <div style={{ width: 1, height: 24, background: 'var(--hairline)' }} />
          <button className="btn btn--sm">Estado <IcChevDown size={12} /></button>
          <button className="btn btn--sm">Índice <IcChevDown size={12} /></button>
        </div>

        {/* Tabs */}
        <div className="row" style={{ gap: 4 }}>
          {([
            { k: 'all', lbl: `Todos · ${counts.all}` },
            { k: 'Active', lbl: `Vigentes · ${counts.Active}` },
            { k: 'Expired', lbl: `Vencidos · ${counts.Expired}` },
            { k: 'Terminated', lbl: `Rescindidos · ${counts.Terminated}` },
          ] as const).map(tab => (
            <button
              key={tab.k}
              onClick={() => handleStatusChange(tab.k as ContractStatus | 'all')}
              className={activeStatus === tab.k ? 'chip chip--solid' : 'chip'}
              style={{ cursor: 'pointer', border: 'none' }}
            >
              {tab.lbl}
            </button>
          ))}
        </div>

        {isLoading && <p className="text-sm text-muted-foreground">Cargando...</p>}
        {error && <p className="text-sm text-destructive">Error al cargar contratos.</p>}

        {/* Tabla */}
        <div className="card" style={{ overflow: 'hidden' }}>
          {filtered.length === 0 && !isLoading ? (
            <div className="text-center py-12 text-muted-foreground">
              <IcDoc size={40} style={{ margin: '0 auto 8px' }} />
              <p>No hay contratos.</p>
            </div>
          ) : (
            <table className="tbl">
              <thead>
                <tr>
                  <th>Contrato</th>
                  <th>Propiedad</th>
                  <th>Índice</th>
                  <th className="num">Alquiler</th>
                  <th>Período</th>
                  <th>Estado</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {paginated.map(c => {
                  const statusInfo = STATUS_LABELS[c.status]
                  const initials = c.appTenantFullName.split(' ').map(s => s[0]).slice(0, 2).join('')
                  return (
                    <tr
                      key={c.id}
                      style={{ cursor: 'pointer' }}
                      onClick={() => navigate(`/admin/contratos/${c.id}`)}
                    >
                      <td>
                        <div className="row" style={{ gap: 10 }}>
                          <div className="mono-avatar">{initials}</div>
                          <div>
                            <div style={{ fontWeight: 500 }}>{c.appTenantFullName}</div>
                            <div className="mono" style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>
                              día {c.dayOfMonth} · {c.currency}
                            </div>
                          </div>
                        </div>
                      </td>
                      <td className="muted">{c.propertyAddress} · {c.propertyCity}</td>
                      <td>
                        <span className={`chip chip--${c.adjustmentType === 'ICL' ? 'icl' : c.adjustmentType === 'IPC' ? 'ipc' : 'info'}`}>
                          <span className="dot" />{ADJ_LABELS[c.adjustmentType]}
                        </span>
                      </td>
                      <td className="num"><b>{formatARS(c.monthlyRent)}</b></td>
                      <td>
                        <div style={{ fontSize: 'var(--fs-xs)' }}>
                          {formatDateShort(c.startDate)} → {formatDateShort(c.endDate)}
                        </div>
                      </td>
                      <td>
                        <span className={`chip ${statusInfo.cls}`}>
                          <span className="dot" />{statusInfo.lbl}
                        </span>
                      </td>
                      <td onClick={e => e.stopPropagation()}>
                        <div className="row" style={{ gap: 4 }}>
                          {c.status === 'Active' && (
                            <button
                              className="btn btn--ghost btn--sm"
                              onClick={() => handleTerminate(c)}
                              disabled={terminate.isPending}
                              title="Rescindir"
                            >
                              ✕
                            </button>
                          )}
                          <button className="btn btn--ghost btn--sm btn--icon">
                            <IcChev size={12} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          )}
          <PaginationBar page={page} totalPages={totalPages} total={filtered.length} pageSize={PAGE_SIZE} onPageChange={setPage} />
        </div>
      </div>
    </>
  )
}
