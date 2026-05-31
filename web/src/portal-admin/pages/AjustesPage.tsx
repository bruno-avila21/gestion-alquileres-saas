import { useState } from 'react'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { IcCalendar, IcDownload } from '@/shared/components/ui/Icons'
import { formatARS, formatDate } from '@/shared/lib/formatters'
import { useAllRentHistory } from '@/features/contracts/hooks/useContracts'
import { contractService } from '@/features/contracts/services/contractService'
import type { AdjustmentType } from '@/features/contracts/types/contract.types'
import { PaginationBar } from '@/shared/components/ui/PaginationBar'

const PAGE_SIZE = 20

const TYPE_CHIP: Record<AdjustmentType, string> = {
  ICL: 'chip--ok',
  IPC: '',
  Manual: 'chip--warn',
}

export default function AjustesPage() {
  const { data: history, isLoading } = useAllRentHistory()
  const [typeFilter, setTypeFilter] = useState<AdjustmentType | 'all'>('all')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(0)

  const filtered = (history ?? []).filter((r) => {
    if (typeFilter !== 'all' && r.adjustmentType !== typeFilter) return false
    if (search) {
      const q = search.toLowerCase()
      return r.contractId.toLowerCase().includes(q) || (r.notes ?? '').toLowerCase().includes(q)
    }
    return true
  })

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE))
  const paginated = filtered.slice(page * PAGE_SIZE, (page + 1) * PAGE_SIZE)

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
          {(['all', 'ICL', 'IPC', 'Manual'] as const).map((t) => (
            <button
              key={t}
              className={`btn btn--sm${typeFilter === t ? ' btn--primary' : ''}`}
              onClick={() => { setTypeFilter(t); setPage(0) }}
            >
              {t === 'all' ? 'Todos' : t}
            </button>
          ))}
          <input
            className="input input--sm"
            style={{ marginLeft: 'auto', width: 220 }}
            placeholder="Buscar por notas…"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(0) }}
          />
        </div>

        {isLoading ? (
          <div className="card" style={{ padding: 48, textAlign: 'center', color: 'var(--muted)' }}>Cargando…</div>
        ) : filtered.length === 0 ? (
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
                {paginated.map((r) => (
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
                    <td className="muted" style={{ fontSize: 'var(--fs-xs)', fontFamily: 'monospace' }}>
                      {r.contractId.slice(0, 8)}…
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
            <PaginationBar page={page} totalPages={totalPages} total={filtered.length} pageSize={PAGE_SIZE} onPageChange={setPage} />
          </div>
        )}
      </div>
    </>
  )
}
