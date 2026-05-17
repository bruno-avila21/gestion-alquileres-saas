import { useNavigate } from 'react-router'
import { useAuthStore } from '@/shared/stores/authStore'
import {
  IcArrowR, IcDownload, IcTrend, IcBuilding, IcCheck, IcHome, IcDoc, IcShield,
} from '@/shared/components/ui/Icons'
import { formatARS } from '@/shared/lib/formatters'

const RECENT_PAYMENTS = [
  { period: 'Abr 2026', amount: 485000, status: 'Pagado' },
  { period: 'Mar 2026', amount: 432000, status: 'Pagado' },
  { period: 'Feb 2026', amount: 432000, status: 'Pagado' },
]

interface BottomNavProps {
  active: 'home' | 'contrato' | 'documentos'
}

function BottomNav({ active }: BottomNavProps) {
  const navigate = useNavigate()
  const items = [
    { k: 'home', label: 'Inicio', icon: <IcHome size={20} />, to: '/inquilino' },
    { k: 'contrato', label: 'Contrato', icon: <IcDoc size={20} />, to: '/inquilino/contrato' },
    { k: 'documentos', label: 'Documentos', icon: <IcShield size={20} />, to: '/inquilino/documentos' },
  ] as const
  return (
    <div
      style={{
        position: 'fixed', bottom: 0, left: 0, right: 0,
        background: 'var(--surface)', borderTop: '1px solid var(--hairline)',
        display: 'flex', justifyContent: 'space-around',
        padding: '8px 0 16px', zIndex: 100,
      }}
    >
      {items.map((item) => (
        <button
          key={item.k}
          onClick={() => navigate(item.to)}
          style={{
            display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 4,
            background: 'none', border: 'none', cursor: 'pointer',
            color: active === item.k ? 'var(--brand)' : 'var(--muted)',
            fontFamily: 'inherit',
          }}
        >
          {item.icon}
          <span style={{ fontSize: 10, fontWeight: 500 }}>{item.label}</span>
        </button>
      ))}
    </div>
  )
}

export default function TenantHomePage() {
  const user = useAuthStore((s) => s.user)
  const name = user?.email?.split('@')[0] ?? 'Inquilino'

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

      {/* Card próximo pago */}
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
            Próximo pago
          </span>
          <span style={{ fontSize: 11, opacity: 0.85 }}>vence en 4 días</span>
        </div>
        <div style={{ fontSize: 30, fontWeight: 600, letterSpacing: '-.02em', marginTop: 6, fontVariantNumeric: 'tabular-nums' }}>
          {formatARS(485000)}
        </div>
        <div style={{ fontSize: 'var(--fs-xs)', opacity: 0.85 }}>Mayo 2026 · vence 10 may</div>
        <div className="row" style={{ marginTop: 14, gap: 6 }}>
          <button
            className="btn"
            style={{ background: 'white', color: 'var(--brand-700)', border: 'none', flex: 1, justifyContent: 'center', fontWeight: 600 }}
          >
            Pagar ahora <IcArrowR size={14} />
          </button>
          <button
            className="btn btn--icon"
            style={{ background: 'rgba(255,255,255,.15)', color: 'white', border: 'none' }}
          >
            <IcDownload size={14} />
          </button>
        </div>
      </div>

      {/* Próximo ajuste */}
      <div className="card" style={{ padding: 14, border: '1px solid var(--brand-100)', background: 'var(--brand-50)' }}>
        <div className="row">
          <div style={{ width: 32, height: 32, borderRadius: 8, background: 'white', color: 'var(--brand)', display: 'grid', placeItems: 'center', flex: '0 0 auto' }}>
            <IcTrend size={16} />
          </div>
          <div style={{ flex: 1 }}>
            <div className="between">
              <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--brand-700)', fontWeight: 500 }}>
                Tu próximo ajuste
              </span>
              <span className="chip chip--icl" style={{ height: 18, fontSize: 10 }}>
                <span className="dot" />ICL
              </span>
            </div>
            <div style={{ fontSize: 18, fontWeight: 600, color: 'var(--brand-700)', marginTop: 2, fontVariantNumeric: 'tabular-nums' }}>
              15 jul · +12,4%
            </div>
            <div style={{ fontSize: 11, color: 'var(--brand-700)', opacity: 0.8, marginTop: 2 }}>
              {formatARS(485000)} → {formatARS(545000)} (estimado)
            </div>
          </div>
        </div>
        <button
          className="btn"
          style={{ marginTop: 10, width: '100%', justifyContent: 'center', background: 'white', border: '1px solid var(--brand-100)', color: 'var(--brand-700)' }}
          onClick={() => window.location.href = '/inquilino/contrato'}
        >
          Cómo se calcula <IcArrowR size={12} />
        </button>
      </div>

      {/* Mi contrato */}
      <div className="card">
        <div className="card-h" style={{ padding: '12px 14px' }}>
          <h3 style={{ fontSize: 14 }}>Tu contrato</h3>
          <span className="chip chip--ok" style={{ height: 18, fontSize: 10 }}>
            <span className="dot" />Vigente
          </span>
        </div>
        <div className="card-b" style={{ padding: '0 14px 14px', display: 'flex', flexDirection: 'column', gap: 10 }}>
          <div className="row" style={{ gap: 10 }}>
            <div style={{ width: 38, height: 38, borderRadius: 7, background: 'var(--surface-3)', color: 'var(--muted)', display: 'grid', placeItems: 'center', flex: '0 0 auto' }}>
              <IcBuilding size={18} />
            </div>
            <div>
              <div style={{ fontSize: 'var(--fs-sm)', fontWeight: 500 }}>Av. Córdoba 2840 · 7B</div>
              <div style={{ fontSize: 11, color: 'var(--muted)' }}>CABA · hasta 15 jul 2027</div>
            </div>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8, marginTop: 4 }}>
            <div className="kv">
              <span className="k" style={{ fontSize: 10 }}>Alquiler vigente</span>
              <span className="v" style={{ fontWeight: 600 }}>{formatARS(485000)}</span>
            </div>
            <div className="kv">
              <span className="k" style={{ fontSize: 10 }}>Próximo</span>
              <span className="v">15 jul 2026</span>
            </div>
          </div>
        </div>
      </div>

      {/* Pagos recientes */}
      <div>
        <div className="between" style={{ padding: '0 4px 8px' }}>
          <span className="sect-title" style={{ margin: 0 }}>Pagos recientes</span>
          <a style={{ fontSize: 11, color: 'var(--brand)', cursor: 'pointer' }}>Ver →</a>
        </div>
        <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
          {RECENT_PAYMENTS.map((r, i) => (
            <div
              key={i}
              className="between"
              style={{ padding: '10px 14px', borderBottom: i < RECENT_PAYMENTS.length - 1 ? '1px solid var(--hairline)' : 'none' }}
            >
              <div className="row">
                <div style={{ width: 28, height: 28, borderRadius: 50, background: 'var(--ok-50)', color: 'var(--ok)', display: 'grid', placeItems: 'center' }}>
                  <IcCheck size={12} />
                </div>
                <div>
                  <div style={{ fontSize: 'var(--fs-sm)', fontWeight: 500 }}>{r.period}</div>
                  <div style={{ fontSize: 11, color: 'var(--muted)' }}>{r.status}</div>
                </div>
              </div>
              <div className="tnum" style={{ fontWeight: 500, fontSize: 'var(--fs-sm)' }}>{formatARS(r.amount)}</div>
            </div>
          ))}
        </div>
      </div>

      <BottomNav active="home" />
    </div>
  )
}
