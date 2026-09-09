import { useState } from 'react'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { IcCalendar, IcDownload } from '@/shared/components/ui/Icons'
import { QueryError } from '@/shared/components/ui/QueryError'
import { formatARS, formatDate } from '@/shared/lib/formatters'
import { useAllRentHistory, useContracts } from '@/features/contracts/hooks/useContracts'
import { contractService } from '@/features/contracts/services/contractService'
import type { AdjustmentType } from '@/features/contracts/types/contract.types'
import { PaginationBar } from '@/shared/components/ui/PaginationBar'
import { useDebounce } from '@/shared/hooks/useDebounce'

const PAGE_SIZE = 20

// Usa los tokens dedicados --icl y --ipc, igual que la pantalla de Contratos. Antes ICL salía
// verde (color reservado para "pagado") e IPC gris neutro, así que el código de color que el
// usuario aprende en una pantalla no se sostenía en la otra.
const TYPE_CHIP: Record<AdjustmentType, string> = {
  ICL: 'chip--icl',
  IPC: 'chip--ipc',
  FixedPercent: 'chip--info',
  Manual: 'chip--warn',
}

const TYPE_LABELS: Record<AdjustmentType, string> = {
  ICL: 'ICL',
  IPC: 'IPC',
  FixedPercent: '% fijo',
  Manual: 'Manual',
}

export default function AjustesPage() {
  const { data: contracts } = useContracts()
  const [typeFilter, setTypeFilter] = useState<AdjustmentType | 'all'>('all')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(0)
  const debouncedSearch = useDebounce(search)

  // Server-side pagination + filter + search (audit M10): the type filter and the tenant/address/notes
  // search run in the API (joined to contract), so they span the whole dataset — not just the loaded
  // page. contractLabel is still used for display in the row.
  const { data, isLoading, isError, refetch } = useAllRentHistory({
    page: page + 1, pageSize: PAGE_SIZE,
    type: typeFilter === 'all' ? undefined : typeFilter,
    search: debouncedSearch || undefined,
  })
  const history = data?.items ?? []
  const total = data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))

  // contractId -> tenant + address, for display (audit B13).
  const contractLabel = new Map(
    (contracts ?? []).map((c) => [c.id, `${c.appTenantFullName} · ${c.propertyAddress}`]),
  )

  return (
    <>
      <AdminTopbar crumbs={['Ajustes de alquiler']} />
      <div className="page">
        <div className="page-h">
          <div>
            <h1>Ajustes</h1>
            <div className="lead">Historial de actualizaciones de alquiler</div>
          </div>
          <button
            className="btn btn--sm"
            onClick={async () => {
              const blob = await contractService.exportAdjustmentsCsv()
              const url = URL.createObjectURL(blob)
              const a = document.createElement('a')
              a.href = url
              a.download = 'ajustes.csv'
              a.click()
              URL.revokeObjectURL(url)
            }}
          >
            <IcDownload size={12} /> Exportar CSV
          </button>
        </div>

        <div className="row" style={{ gap: 8, flexWrap: 'wrap' }}>
          {(['all', 'ICL', 'IPC', 'FixedPercent', 'Manual'] as const).map((t) => (
            <button
              key={t}
              className={`btn btn--sm${typeFilter === t ? ' btn--primary' : ''}`}
              onClick={() => { setTypeFilter(t); setPage(0) }}
            >
              {t === 'all' ? 'Todos' : TYPE_LABELS[t]}
            </button>
          ))}
          <input
            className="input input--sm"
            style={{ marginLeft: 'auto', width: 220 }}
            placeholder="Buscar por inquilino, dirección o notas…"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(0) }}
          />
        </div>

        {isLoading ? (
          <div className="card" style={{ padding: 48, textAlign: 'center', color: 'var(--muted)' }}>Cargando…</div>
        ) : isError ? (
          <QueryError onRetry={() => refetch()} message="No pudimos cargar los ajustes." />
        ) : history.length === 0 ? (
          <div className="card" style={{ padding: 48, textAlign: 'center', color: 'var(--muted)' }}>
            <IcCalendar size={32} style={{ margin: '0 auto 8px', display: 'block' }} />
            Sin ajustes registrados
          </div>
        ) : (
          <div className="card">
            <table className="tbl">
              <thead>
                <tr>
                  <th>Tipo</th>
                  <th className="num">Alquiler anterior</th>
                  <th className="num">Nuevo alquiler</th>
                  <th className="num">Factor</th>
                  <th>Vigencia</th>
                  <th>Contrato</th>
                  <th>Notas</th>
                  <th>Registrado</th>
                </tr>
              </thead>
              <tbody>
                {history.map((r) => (
                  <tr key={r.id}>
                    <td>
                      <span className={`chip ${TYPE_CHIP[r.adjustmentType]}`}>
                        <span className="dot" />
                        {r.adjustmentType}
                      </span>
                    </td>
                    <td className="num">{formatARS(r.previousRent)}</td>
                    <td className="num"><b>{formatARS(r.newRent)}</b></td>
                    <td className="num" style={{ color: 'var(--muted)', fontSize: 'var(--fs-xs)' }}>
                      ×{r.adjustmentFactor.toFixed(4)}
                    </td>
                    <td>{formatDate(r.effectiveDate)}</td>
                    <td className="muted" style={{ fontSize: 'var(--fs-xs)', maxWidth: 220, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                      {contractLabel.get(r.contractId) ?? `${r.contractId.slice(0, 8)}…`}
                    </td>
                    <td className="muted" style={{ fontSize: 'var(--fs-xs)', maxWidth: 180, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                      {r.notes ?? '—'}
                    </td>
                    <td className="muted" style={{ fontSize: 'var(--fs-xs)' }}>
                      {formatDate(r.createdAt.split('T')[0])}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <PaginationBar page={page} totalPages={totalPages} total={total} pageSize={PAGE_SIZE} onPageChange={setPage} />
          </div>
        )}
      </div>
    </>
  )
}
