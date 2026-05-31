import { useState } from 'react'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { IcCash, IcDownload } from '@/shared/components/ui/Icons'
import { formatARS, formatDate, formatPeriod } from '@/shared/lib/formatters'
import { useAllTransactions } from '@/features/contracts/hooks/useContracts'
import { contractService } from '@/features/contracts/services/contractService'
import type { TransactionType } from '@/features/contracts/types/contract.types'
import { PaginationBar } from '@/shared/components/ui/PaginationBar'

const PAGE_SIZE = 20

const TX_LABELS: Record<TransactionType, string> = {
  RentCharge: 'Cargo alquiler',
  Payment: 'Pago',
  ManualDebit: 'Débito manual',
  ManualCredit: 'Crédito manual',
}

const TX_CHIP: Record<TransactionType, string> = {
  RentCharge: '',
  Payment: 'chip--ok',
  ManualDebit: 'chip--danger',
  ManualCredit: 'chip--warn',
}

type Filter = 'all' | TransactionType

export default function PagosPage() {
  const { data: transactions, isLoading } = useAllTransactions()
  const [filter, setFilter] = useState<Filter>('all')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(0)

  const filtered = (transactions ?? []).filter((t) => {
    if (filter !== 'all' && t.type !== filter) return false
    if (search) {
      const q = search.toLowerCase()
      return (
        t.contractId.toLowerCase().includes(q) ||
        (t.notes ?? '').toLowerCase().includes(q)
      )
    }
    return true
  })

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE))
  const paginated = filtered.slice(page * PAGE_SIZE, (page + 1) * PAGE_SIZE)

  const total = filtered.reduce((sum, t) => {
    if (t.type === 'Payment' || t.type === 'ManualCredit') return sum + t.amount
    return sum - t.amount
  }, 0)

  const FILTERS: { key: Filter; label: string }[] = [
    { key: 'all', label: 'Todos' },
    { key: 'Payment', label: 'Pagos' },
    { key: 'RentCharge', label: 'Cargos' },
    { key: 'ManualDebit', label: 'Débitos' },
    { key: 'ManualCredit', label: 'Créditos' },
  ]

  return (
    <>
      <AdminTopbar crumbs={['Pagos y transacciones']} />
      <div className="page">
        <div className="page-h">
          <div>
            <h1>Pagos</h1>
            <div className="lead">Todas las transacciones de la organización</div>
          </div>
          <button
            className="btn btn--sm"
            onClick={async () => {
              const blob = await contractService.exportTransactionsCsv()
              const url = URL.createObjectURL(blob)
              const a = document.createElement('a')
              a.href = url
              a.download = 'transacciones.csv'
              a.click()
              URL.revokeObjectURL(url)
            }}
          >
            <IcDownload size={12} /> Exportar CSV
          </button>
        </div>

        <div className="row" style={{ gap: 8, flexWrap: 'wrap' }}>
          {FILTERS.map((f) => (
            <button
              key={f.key}
              className={`btn btn--sm${filter === f.key ? ' btn--primary' : ''}`}
              onClick={() => { setFilter(f.key); setPage(0) }}
            >
              {f.label}
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
            <IcCash size={32} style={{ margin: '0 auto 8px', display: 'block' }} />
            Sin transacciones
          </div>
        ) : (
          <div className="card">
            <table className="tbl">
              <thead>
                <tr>
                  <th>Tipo</th>
                  <th>Período</th>
                  <th className="num">Importe</th>
                  <th>Moneda</th>
                  <th>Contrato</th>
                  <th>Notas</th>
                  <th>Fecha</th>
                </tr>
              </thead>
              <tbody>
                {paginated.map((t) => (
                  <tr key={t.id}>
                    <td>
                      <span className={`chip ${TX_CHIP[t.type]}`}>
                        <span className="dot" />
                        {TX_LABELS[t.type]}
                      </span>
                    </td>
                    <td>{formatPeriod(t.period)}</td>
                    <td className="num"><b>{formatARS(t.amount)}</b></td>
                    <td>{t.currency}</td>
                    <td className="muted" style={{ fontSize: 'var(--fs-xs)', fontFamily: 'monospace' }}>
                      {t.contractId.slice(0, 8)}…
                    </td>
                    <td className="muted" style={{ fontSize: 'var(--fs-xs)', maxWidth: 200, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                      {t.notes ?? '—'}
                    </td>
                    <td className="muted" style={{ fontSize: 'var(--fs-xs)' }}>
                      {formatDate(t.createdAt.split('T')[0])}
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <td colSpan={2} style={{ fontWeight: 600, paddingLeft: 12 }}>Balance neto</td>
                  <td className="num" style={{ fontWeight: 700, color: total >= 0 ? 'var(--ok)' : 'var(--danger)' }}>
                    {formatARS(Math.abs(total))}
                  </td>
                  <td colSpan={4} />
                </tr>
              </tfoot>
            </table>
            <PaginationBar page={page} totalPages={totalPages} total={filtered.length} pageSize={PAGE_SIZE} onPageChange={setPage} />
          </div>
        )}
      </div>
    </>
  )
}
