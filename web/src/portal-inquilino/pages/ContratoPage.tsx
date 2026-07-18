import { useNavigate } from 'react-router'
import { IcArrowUp, IcShield, IcDownload, IcPhone, IcHome, IcDoc, IcCash } from '@/shared/components/ui/Icons'
import { QueryError } from '@/shared/components/ui/QueryError'
import { formatARS, formatDate } from '@/shared/lib/formatters'
import { useMyContract, useMyTransactions, useMyRentHistory } from '@/features/me/hooks/useMe'

function BottomNav({ active }: { active: 'home' | 'contrato' | 'documentos' | 'pagos' }) {
  const navigate = useNavigate()
  const items = [
    { k: 'home', label: 'Inicio', to: '/inquilino' },
    { k: 'contrato', label: 'Contrato', to: '/inquilino/contrato' },
    { k: 'documentos', label: 'Docs', to: '/inquilino/documentos' },
    { k: 'pagos', label: 'Pagos', to: '/inquilino/pagos' },
  ] as const
  return (
    <div style={{ position: 'fixed', bottom: 0, left: 0, right: 0, background: 'var(--surface)', borderTop: '1px solid var(--hairline)', display: 'flex', justifyContent: 'space-around', padding: '8px 0 16px', zIndex: 100 }}>
      {items.map((item) => (
        <button key={item.k} onClick={() => navigate(item.to)} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 4, background: 'none', border: 'none', cursor: 'pointer', color: active === item.k ? 'var(--brand)' : 'var(--muted)', fontFamily: 'inherit' }}>
          {item.k === 'home' ? <IcHome size={20} /> : item.k === 'contrato' ? <IcDoc size={20} /> : item.k === 'documentos' ? <IcShield size={20} /> : <IcCash size={20} />}
          <span style={{ fontSize: 10, fontWeight: 500 }}>{item.label}</span>
        </button>
      ))}
    </div>
  )
}

export default function TenantContratoPage() {
  const navigate = useNavigate()
  const { data: contract, isLoading, isError, refetch } = useMyContract()
  const { data: transactions } = useMyTransactions()
  const { data: rentHistory } = useMyRentHistory()

  const adjLabel = contract?.adjustmentType === 'ICL' ? 'ICL' : contract?.adjustmentType === 'IPC' ? 'IPC' : 'Manual'
  const freqLabel = contract?.adjustmentFrequency === 'Monthly' ? 'mensual' : contract?.adjustmentFrequency === 'Quarterly' ? 'trimestral' : 'anual'

  const payments = (transactions ?? []).filter(t => t.type === 'Payment')
  const adjustments = rentHistory ?? []

  if (isLoading) {
    return (
      <div style={{ maxWidth: 420, margin: '0 auto', padding: '40px 18px 80px', textAlign: 'center', color: 'var(--muted)' }}>
        Cargando contrato…
        <BottomNav active="contrato" />
      </div>
    )
  }

  if (isError) {
    return (
      <div style={{ maxWidth: 420, margin: '0 auto', padding: '40px 18px 80px' }}>
        <QueryError onRetry={() => refetch()} message="No pudimos cargar tu contrato." />
        <BottomNav active="contrato" />
      </div>
    )
  }

  if (!contract) {
    return (
      <div style={{ maxWidth: 420, margin: '0 auto', padding: '40px 18px 80px' }}>
        <div className="card" style={{ padding: 20, textAlign: 'center', color: 'var(--muted)' }}>
          Sin contrato activo. Contactá a tu administrador.
        </div>
        <BottomNav active="contrato" />
      </div>
    )
  }

  return (
    <div style={{ maxWidth: 420, margin: '0 auto', padding: '20px 18px 80px', display: 'flex', flexDirection: 'column', gap: 14 }}>
      {/* Header */}
      <div>
        <div className="row" style={{ gap: 6 }}>
          <span className={`chip chip--${contract.status === 'Active' ? 'ok' : 'warn'}`} style={{ height: 18, fontSize: 10 }}>
            <span className="dot" />{contract.status === 'Active' ? 'Vigente' : contract.status}
          </span>
          <span className={`chip chip--${adjLabel === 'ICL' ? 'icl' : adjLabel === 'IPC' ? 'ipc' : 'warn'}`} style={{ height: 18, fontSize: 10 }}>
            <span className="dot" />{adjLabel}
          </span>
        </div>
        <h2 style={{ fontSize: 20, fontWeight: 600, margin: '6px 0 2px', letterSpacing: '-.01em' }}>Tu contrato</h2>
        <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>{formatDate(contract.startDate)} — {formatDate(contract.endDate)}</div>
      </div>

      {/* Datos del contrato */}
      <div className="card card-b" style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
        {[
          { k: 'Propiedad', v: `${contract.propertyAddress}, ${contract.propertyCity}` },
          { k: 'Inquilino', v: contract.appTenantFullName },
          { k: 'Día de pago', v: `${contract.dayOfMonth} de cada mes` },
          { k: 'Indexación', v: `${adjLabel} · ${freqLabel}` },
          { k: 'Moneda', v: contract.currency },
          ...(contract.depositAmount != null ? [{ k: 'Depósito', v: formatARS(contract.depositAmount) }] : []),
        ].map((kv) => (
          <div key={kv.k} className="kv">
            <span className="k">{kv.k}</span>
            <span className="v">{kv.v}</span>
          </div>
        ))}
      </div>

      {/* Stats */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8 }}>
        <div className="card" style={{ padding: '12px 14px' }}>
          <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', textTransform: 'uppercase', letterSpacing: '.04em' }}>
            Alquiler vigente
          </div>
          <div style={{ fontSize: 18, fontWeight: 600, marginTop: 4, fontVariantNumeric: 'tabular-nums' }}>
            {formatARS(contract.monthlyRent)}
          </div>
        </div>
        <div className="card" style={{ padding: '12px 14px', background: 'var(--brand-50)', borderColor: 'var(--brand-100)' }}>
          <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--brand-700)', textTransform: 'uppercase', letterSpacing: '.04em' }}>
            Pagos registrados
          </div>
          <div style={{ fontSize: 18, fontWeight: 600, marginTop: 4, color: 'var(--brand-700)', fontVariantNumeric: 'tabular-nums' }}>
            <IcArrowUp size={14} /> {payments.length}
          </div>
        </div>
      </div>

      {/* Ajustes */}
      {adjustments.length > 0 && (
        <div className="card" style={{ padding: 14 }}>
          <div className="sect-title" style={{ marginBottom: 10 }}>Historial de ajustes ({adjustments.length})</div>
          {adjustments.slice(0, 4).map((a, i) => (
            <div
              key={a.id}
              className="between"
              style={{ marginBottom: i < adjustments.slice(0, 4).length - 1 ? 10 : 0 }}
            >
              <div>
                <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>
                  {formatDate(a.effectiveDate)} · {a.adjustmentType}
                </div>
                <div style={{ fontSize: 11, color: 'var(--muted)', marginTop: 1 }}>
                  ×{a.adjustmentFactor.toFixed(4)}
                </div>
              </div>
              <div style={{ textAlign: 'right' }}>
                <div style={{ fontSize: 'var(--fs-sm)', fontWeight: 500 }}>{formatARS(a.newRent)}</div>
                <div style={{ fontSize: 11, color: 'var(--muted)' }}>antes: {formatARS(a.previousRent)}</div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Acciones */}
      <button
        className="btn"
        style={{ width: '100%', justifyContent: 'center' }}
        onClick={() => navigate('/inquilino/documentos')}
      >
        <IcDownload size={14} /> Ver documentos del contrato
      </button>
      <button
        className="btn btn--ghost"
        style={{ width: '100%', justifyContent: 'center', color: 'var(--muted)', cursor: 'not-allowed' }}
        disabled
        title="Próximamente"
      >
        <IcPhone size={14} /> Solicitar ayuda (próximamente)
      </button>

      <BottomNav active="contrato" />
    </div>
  )
}
