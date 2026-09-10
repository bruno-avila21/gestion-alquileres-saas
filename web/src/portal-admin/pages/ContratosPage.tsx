import { useState } from 'react'
import { useNavigate } from 'react-router'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { useContracts, useCreateContract, useTerminateContract } from '@/features/contracts/hooks/useContracts'
import { useProperties } from '@/features/properties/hooks/useProperties'
import { useAppTenants } from '@/features/apptenants/hooks/useAppTenants'
import type { AdjustmentType, ContractDto, ContractStatus } from '@/features/contracts/types/contract.types'
import {
  IcPlus, IcDownload, IcChev, IcDoc,
} from '@/shared/components/ui/Icons'
import { formatARS, formatDateShort } from '@/shared/lib/formatters'
import { PaginationBar } from '@/shared/components/ui/PaginationBar'
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog'
import { QueryError } from '@/shared/components/ui/QueryError'
import { downloadCsv } from '@/shared/lib/exportCsv'
import { SearchInput } from '@/shared/components/ui/SearchInput'
import { ContratoFormFields } from '@/features/contracts/components/ContratoFormFields'
import {
  EMPTY_CONTRACT_FORM, contractFormToRequest, type ContractFormState,
} from '@/features/contracts/utils/contractForm'

const PAGE_SIZE = 20

const STATUS_LABELS: Record<ContractStatus, { cls: string; lbl: string }> = {
  Active: { cls: 'chip--ok', lbl: 'Vigente' },
  Expired: { cls: 'chip--warn', lbl: 'Vencido' },
  Terminated: { cls: 'chip--danger', lbl: 'Rescindido' },
}

const ADJ_LABELS: Record<AdjustmentType, string> = {
  ICL: 'ICL',
  IPC: 'IPC',
  Manual: 'Manual',
  FixedPercent: '% fijo',
}

/** Opciones del filtro por índice. Se derivan de ADJ_LABELS para que agregar un tipo de
    ajuste no deje el filtro atrasado. */
const ADJ_OPTIONS: { value: AdjustmentType | 'all'; label: string }[] = [
  { value: 'all', label: 'Todos los índices' },
  ...(Object.keys(ADJ_LABELS) as AdjustmentType[]).map(k => ({ value: k, label: ADJ_LABELS[k] })),
]

export default function ContratosPage() {
  const navigate = useNavigate()
  const { data: contracts, isLoading, error } = useContracts()
  const { data: properties } = useProperties()
  const { data: tenants } = useAppTenants()
  const create = useCreateContract()
  const terminate = useTerminateContract()

  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState<ContractFormState>(EMPTY_CONTRACT_FORM)
  const [search, setSearch] = useState('')
  const [activeStatus, setActiveStatus] = useState<ContractStatus | 'all'>('all')
  const [adjType, setAdjType] = useState<AdjustmentType | 'all'>('all')
  const [formErr, setFormErr] = useState('')
  const [page, setPage] = useState(0)
  const [confirmTerminate, setConfirmTerminate] = useState<ContractDto | null>(null)
  const [actionErr, setActionErr] = useState<string | null>(null)

  /* Búsqueda e índice se aplican antes que el estado porque los números de las solapas se
     cuentan acá: si no, filtrar por ICL deja "Vigentes · 10" arriba de una tabla de 4. */
  const enScope = (contracts ?? []).filter(c => {
    const matchAdj = adjType === 'all' || c.adjustmentType === adjType
    const q = search.toLowerCase()
    const matchSearch = !q || c.appTenantFullName.toLowerCase().includes(q) || c.propertyAddress.toLowerCase().includes(q)
    return matchAdj && matchSearch
  })

  const filtered = enScope.filter(c => activeStatus === 'all' || c.status === activeStatus)

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE))
  const paginated = filtered.slice(page * PAGE_SIZE, (page + 1) * PAGE_SIZE)

  function handleSearch(val: string) { setSearch(val); setPage(0) }
  function handleStatusChange(status: ContractStatus | 'all') { setActiveStatus(status); setPage(0) }
  function handleAdjChange(t: AdjustmentType | 'all') { setAdjType(t); setPage(0) }

  const counts = {
    all: enScope.length,
    Active: enScope.filter(c => c.status === 'Active').length,
    Expired: enScope.filter(c => c.status === 'Expired').length,
    Terminated: enScope.filter(c => c.status === 'Terminated').length,
  }

  function openCreate() {
    setForm(EMPTY_CONTRACT_FORM)
    setFormErr('')
    setShowForm(true)
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setFormErr('')
    try {
      await create.mutateAsync(contractFormToRequest(form))
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

  function handleExport() {
    downloadCsv(
      'contratos.csv',
      ['Inquilino', 'Propiedad', 'Alquiler', 'Moneda', 'Ajuste', 'Estado', 'Inicio', 'Fin'],
      filtered.map((c) => [
        c.appTenantFullName, c.propertyAddress, c.monthlyRent, c.currency,
        ADJ_LABELS[c.adjustmentType], STATUS_LABELS[c.status].lbl, c.startDate, c.endDate,
      ]),
    )
  }

  return (
    <>
      <AdminTopbar
        crumbs={['Contratos']}
        right={
          <button className="btn btn--sm btn--primary" onClick={openCreate}>
            <IcPlus size={12} /> Nuevo contrato
          </button>
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
          <button className="btn btn--sm" onClick={handleExport} disabled={filtered.length === 0}>
            <IcDownload size={12} /> Exportar
          </button>
        </div>

        {/* Form */}
        {showForm && (
          <div className="card" style={{ padding: 16, display: 'flex', flexDirection: 'column', gap: 14, maxWidth: 720 }}>
            <h2 style={{ fontWeight: 600, margin: 0 }}>Nuevo contrato</h2>
            {formErr && <div role="alert" style={{ fontSize: 'var(--fs-sm)', color: 'var(--danger)' }}>{formErr}</div>}
            <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              <ContratoFormFields
                form={form}
                setForm={setForm}
                properties={properties}
                tenants={tenants}
              />
              <div className="row" style={{ gap: 8, justifyContent: 'flex-end' }}>
                <button type="button" className="btn btn--sm" onClick={() => setShowForm(false)}>Cancelar</button>
                <button type="submit" className="btn btn--sm btn--primary" disabled={create.isPending}>Crear contrato</button>
              </div>
            </form>
          </div>
        )}

        {/* Filtros */}
        <div className="card card-b" style={{ display: 'flex', gap: 10, alignItems: 'center', padding: '12px 14px' }}>
          <SearchInput
            grow
            value={search}
            onChange={handleSearch}
            placeholder="Buscar por inquilino o dirección…"
            ariaLabel="Buscar contratos"
          />
          <div style={{ width: 1, height: 24, background: 'var(--hairline)' }} />
          {/* El estado no va acá: lo filtran las solapas de abajo, que además muestran el
              conteo. Dos controles para lo mismo es peor que uno. */}
          <select
            className="select select--inline"
            aria-label="Filtrar por índice de ajuste"
            value={adjType}
            onChange={e => handleAdjChange(e.target.value as AdjustmentType | 'all')}
          >
            {ADJ_OPTIONS.map(o => (
              <option key={o.value} value={o.value}>{o.label}</option>
            ))}
          </select>
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

        {error && <QueryError message="Error al cargar contratos." />}

        {/* Tabla */}
        <div className="card" style={{ overflow: 'hidden' }}>
          {isLoading ? (
            <div style={{ padding: 48, textAlign: 'center', color: 'var(--muted)' }}>Cargando…</div>
          ) : filtered.length === 0 ? (
            <div style={{ padding: 48, textAlign: 'center', color: 'var(--muted)' }}>
              <IcDoc size={32} style={{ margin: '0 auto 8px', display: 'block' }} />
              {(contracts?.length ?? 0) === 0
                ? 'No hay contratos.'
                : 'Ningún contrato coincide con los filtros.'}
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
                            <div style={{ fontWeight: 500, whiteSpace: 'nowrap' }}>{c.appTenantFullName}</div>
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
                        <div style={{ fontSize: 'var(--fs-xs)', whiteSpace: 'nowrap' }}>
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
