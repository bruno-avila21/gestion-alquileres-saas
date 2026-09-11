import { useNavigate } from 'react-router'
import { useAuthStore } from '@/shared/stores/authStore'
import {
  IcArrowR, IcDownload, IcTrend, IcBuilding, IcCheck,
} from '@/shared/components/ui/Icons'
import { QueryError } from '@/shared/components/ui/QueryError'
import { formatARS, formatDate, formatPeriod } from '@/shared/lib/formatters'
import { useMyContract, useMyTransactions } from '@/features/me/hooks/useMe'

export default function TenantHomePage() {
  const navigate = useNavigate()
  const user = useAuthStore((s) => s.user)
  const name = user?.email?.split('@')[0] ?? 'Inquilino'
  const { data: contract, isLoading, isError, refetch } = useMyContract()
  const { data: transactions } = useMyTransactions()

  const recentPayments = (transactions ?? []).filter(t => t.type === 'Payment').slice(0, 3)

  return (
    <div style={{ maxWidth: 420, margin: '0 auto', padding: '20px 18px 80px', display: 'flex', flexDirection: 'column', gap: 14 }}>
      {/* Saludo */}
      <div className="between">
        <div>
          <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>Hola de nuevo</div>
          <div style={{ fontSize: 22, fontWeight: 600, letterSpacing: '-.01em', marginTop: 2 }}>
            {name}
          </div>
        </div>
        <div
          className="mono-avatar"
          style={{ width: 36, height: 36, fontSize: 12, background: 'var(--brand-50)', color: 'var(--brand-700)', borderColor: 'var(--brand-100)' }}
        >
          {name.slice(0, 2).toUpperCase()}
        </div>
      </div>

      {/* Card alquiler vigente */}
      {isLoading ? (
        <div className="card" style={{ padding: 20, textAlign: 'center', color: 'var(--muted)' }}>Cargando…</div>
      ) : isError ? (
        <QueryError onRetry={() => refetch()} message="No pudimos cargar tu contrato." />
      ) : contract ? (
        <div
          className="card"
          style={{
            padding: 16,
            background: 'linear-gradient(160deg, var(--brand) 0%, var(--brand-700) 100%)',
            color: 'white',
            border: 'none',
          }}
        >
          <div className="between">
            <span style={{ fontSize: 11, textTransform: 'uppercase', letterSpacing: '.06em', opacity: 0.8, fontWeight: 500 }}>
              Alquiler vigente
            </span>
            <span className={`chip ${contract.status === 'Active' ? 'chip--ok' : 'chip--warn'}`} style={{ height: 18, fontSize: 10 }}>
              <span className="dot" />{contract.status === 'Active' ? 'Vigente' : contract.status}
            </span>
          </div>
          <div style={{ fontSize: 30, fontWeight: 600, letterSpacing: '-.02em', marginTop: 6, fontVariantNumeric: 'tabular-nums' }}>
            {formatARS(contract.monthlyRent)}
          </div>
          <div style={{ fontSize: 'var(--fs-xs)', opacity: 0.85 }}>{contract.currency} · vence {formatDate(contract.endDate)}</div>
          <div className="row" style={{ marginTop: 14, gap: 6 }}>
            <button
              className="btn"
              style={{ background: 'white', color: 'var(--brand-700)', border: 'none', flex: 1, justifyContent: 'center', fontWeight: 600 }}
              onClick={() => navigate('/inquilino/contrato')}
            >
              Ver contrato <IcArrowR size={14} />
            </button>
            <button
              className="btn btn--icon"
              style={{ background: 'rgba(255,255,255,.15)', color: 'white', border: 'none' }}
              onClick={() => navigate('/inquilino/documentos')}
              aria-label="Ver documentos"
              title="Ver documentos"
            >
              <IcDownload size={14} />
            </button>
          </div>
        </div>
      ) : (
        <div className="card" style={{ padding: 16 }}>
          <div style={{ color: 'var(--muted)', fontSize: 'var(--fs-sm)' }}>Sin contrato activo en esta organización.</div>
        </div>
      )}

      {/* Ajuste */}
      {contract && (
        <div className="card" style={{ padding: 14, border: '1px solid var(--brand-100)', background: 'var(--brand-50)' }}>
          <div className="row">
            <div style={{ width: 32, height: 32, borderRadius: 8, background: 'white', color: 'var(--brand)', display: 'grid', placeItems: 'center', flex: '0 0 auto' }}>
              <IcTrend size={16} />
            </div>
            <div style={{ flex: 1 }}>
              <div className="between">
                <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--brand-700)', fontWeight: 500 }}>Tipo de ajuste</span>
                <span className={`chip chip--${contract.adjustmentType === 'ICL' ? 'icl' : contract.adjustmentType === 'IPC' ? 'ipc' : 'warn'}`} style={{ height: 18, fontSize: 10 }}>
                  <span className="dot" />{contract.adjustmentType}
                </span>
              </div>
              <div style={{ fontSize: 16, fontWeight: 600, color: 'var(--brand-700)', marginTop: 2 }}>
                {contract.adjustmentFrequency === 'Monthly' ? 'Mensual' : contract.adjustmentFrequency === 'Quarterly' ? 'Trimestral' : 'Anual'}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Tu contrato */}
      {contract && (
        <div className="card">
          <div className="card-h" style={{ padding: '12px 14px' }}>
            <h3 style={{ fontSize: 14 }}>Tu propiedad</h3>
          </div>
          <div className="card-b" style={{ padding: '0 14px 14px', display: 'flex', flexDirection: 'column', gap: 10 }}>
            <div className="row" style={{ gap: 10 }}>
              <div style={{ width: 38, height: 38, borderRadius: 7, background: 'var(--surface-3)', color: 'var(--muted)', display: 'grid', placeItems: 'center', flex: '0 0 auto' }}>
                <IcBuilding size={18} />
              </div>
              <div>
                <div style={{ fontSize: 'var(--fs-sm)', fontWeight: 500 }}>{contract.propertyAddress}</div>
                <div style={{ fontSize: 11, color: 'var(--muted)' }}>{contract.propertyCity} · hasta {formatDate(contract.endDate)}</div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Pagos recientes */}
      <div>
        <div className="between" style={{ padding: '0 4px 8px' }}>
          <span className="sect-title" style={{ margin: 0 }}>Pagos registrados</span>
        </div>
        <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
          {recentPayments.length === 0 ? (
            <div style={{ padding: '16px 14px', color: 'var(--muted)', fontSize: 'var(--fs-sm)' }}>Sin pagos registrados aún.</div>
          ) : recentPayments.map((t, i) => (
            <div
              key={t.id}
              className="between"
              style={{ padding: '10px 14px', borderBottom: i < recentPayments.length - 1 ? '1px solid var(--hairline)' : 'none' }}
            >
              <div className="row">
                <div style={{ width: 28, height: 28, borderRadius: 50, background: 'var(--ok-50)', color: 'var(--ok)', display: 'grid', placeItems: 'center' }}>
                  <IcCheck size={12} />
                </div>
                <div>
                  <div style={{ fontSize: 'var(--fs-sm)', fontWeight: 500 }}>{formatPeriod(t.period)}</div>
                  <div style={{ fontSize: 11, color: 'var(--muted)' }}>{t.notes ?? 'Pago registrado'}</div>
                </div>
              </div>
              <div className="tnum" style={{ fontWeight: 500, fontSize: 'var(--fs-sm)' }}>{formatARS(t.amount)}</div>
            </div>
          ))}
        </div>
      </div>

    </div>
  )
}
