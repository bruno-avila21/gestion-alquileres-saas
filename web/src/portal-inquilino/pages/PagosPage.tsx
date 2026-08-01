import { useState } from 'react'
import { IcCash } from '@/shared/components/ui/Icons'
import { QueryError } from '@/shared/components/ui/QueryError'
import { formatARS, formatDate, formatPeriod } from '@/shared/lib/formatters'
import { useMyTransactions } from '@/features/me/hooks/useMe'
import type { TransactionType } from '@/features/contracts/types/contract.types'

type Filter = 'all' | TransactionType

const TX_LABELS: Record<TransactionType, string> = {
  RentCharge: 'Cargo',
  Payment: 'Pago',
  ManualDebit: 'Débito',
  ManualCredit: 'Crédito',
}

function txBg(type: TransactionType): string {
  if (type === 'Payment' || type === 'ManualCredit') return 'var(--ok-50)'
  return 'var(--surface-3)'
}

function txFg(type: TransactionType): string {
  if (type === 'Payment' || type === 'ManualCredit') return 'var(--ok)'
  if (type === 'ManualDebit') return 'var(--danger)'
  return 'var(--muted)'
}

export default function TenantPagosPage() {
  const { data: transactions, isLoading, isError, refetch } = useMyTransactions()
  const [filter, setFilter] = useState<Filter>('all')

  const all = transactions ?? []

  const filtered = all.filter((t) => filter === 'all' || t.type === filter)

  // Net balance over ALL transactions, not the visible filter (audit M6) — otherwise the balance
  // jumped every time the tab changed. Cash-ledger model, consistent with the backend (audit A1):
  // money in (payments + credits) minus what's owed (charges), ignoring cancelled charges.
  const net = all.reduce((sum, t) => {
    if (t.type === 'Payment' || t.type === 'ManualCredit') return sum + t.amount
    if (t.status === 'Cancelled') return sum
    return sum - t.amount
  }, 0)

  const FILTERS: { k: Filter; label: string }[] = [
    { k: 'all', label: 'Todos' },
    { k: 'Payment', label: 'Pagos' },
    { k: 'RentCharge', label: 'Cargos' },
    { k: 'ManualDebit', label: 'Débitos' },
    { k: 'ManualCredit', label: 'Créditos' },
  ]

  return (
    <div style={{ maxWidth: 420, margin: '0 auto', padding: '20px 18px 80px', display: 'flex', flexDirection: 'column', gap: 14 }}>
      <div>
        <h2 style={{ fontSize: 20, fontWeight: 600, margin: 0, letterSpacing: '-.01em' }}>Mis pagos</h2>
        <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', marginTop: 2 }}>
          Todas las transacciones de tu contrato
        </div>
      </div>

      {/* Net balance */}
      <div
        className="card"
        style={{ padding: '14px 16px', border: '1px solid var(--brand-100)', background: 'var(--brand-50)' }}
      >
        <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--brand-700)', textTransform: 'uppercase', letterSpacing: '.04em' }}>
          Balance neto
        </div>
        <div
          style={{
            fontSize: 22, fontWeight: 600, marginTop: 4,
            color: net >= 0 ? 'var(--ok)' : 'var(--danger)',
            fontVariantNumeric: 'tabular-nums',
          }}
        >
          {net < 0 ? '-' : ''}{formatARS(Math.abs(net))}
        </div>
      </div>

      {/* Filter buttons */}
      <div className="row" style={{ gap: 6, flexWrap: 'wrap' }}>
        {FILTERS.map((f) => (
          <button
            key={f.k}
            className={`btn btn--sm${filter === f.k ? ' btn--primary' : ''}`}
            onClick={() => setFilter(f.k)}
          >
            {f.label}
          </button>
        ))}
      </div>

      {/* Transaction list */}
      {isLoading ? (
        <div className="card" style={{ padding: '24px 14px', textAlign: 'center', color: 'var(--muted)' }}>
          Cargando…
        </div>
      ) : isError ? (
        <QueryError onRetry={() => refetch()} message="No pudimos cargar tus transacciones." />
      ) : filtered.length === 0 ? (
        <div className="card" style={{ padding: '28px 14px', textAlign: 'center', color: 'var(--muted)' }}>
          <IcCash size={28} style={{ opacity: 0.3, display: 'block', margin: '0 auto 8px' }} />
          Sin transacciones
        </div>
      ) : (
        <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
          {filtered.map((t, i) => (
            <div
              key={t.id}
              className="between"
              style={{
                padding: '11px 14px',
                borderBottom: i < filtered.length - 1 ? '1px solid var(--hairline)' : 'none',
              }}
            >
              <div className="row" style={{ gap: 10, minWidth: 0 }}>
                <div
                  style={{
                    width: 30, height: 30, borderRadius: 8, flexShrink: 0,
                    background: txBg(t.type), color: txFg(t.type),
                    display: 'grid', placeItems: 'center',
                  }}
                >
                  <IcCash size={14} />
                </div>
                <div style={{ minWidth: 0 }}>
                  <div style={{ fontSize: 'var(--fs-sm)', fontWeight: 500 }}>{TX_LABELS[t.type]}</div>
                  <div style={{ fontSize: 11, color: 'var(--muted)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                    {formatPeriod(t.period)}{t.notes ? ` · ${t.notes}` : ''}
                  </div>
                </div>
              </div>
              <div
                style={{
                  fontWeight: 600, fontSize: 'var(--fs-sm)',
                  fontVariantNumeric: 'tabular-nums', flexShrink: 0, marginLeft: 8,
                  color: t.type === 'Payment' || t.type === 'ManualCredit' ? 'var(--ok)' : 'var(--ink)',
                }}
              >
                {t.type === 'Payment' || t.type === 'ManualCredit' ? '+' : '−'}{formatARS(t.amount)}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Last payment date */}
      {!isLoading && filtered.length > 0 && (
        <div style={{ textAlign: 'center', fontSize: 11, color: 'var(--muted)' }}>
          Último registro: {formatDate(filtered[0].createdAt.split('T')[0])}
        </div>
      )}

    </div>
  )
}
