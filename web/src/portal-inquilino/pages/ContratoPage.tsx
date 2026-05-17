import { useNavigate } from 'react-router'
import { IcArrowUp, IcShield, IcDownload, IcPhone, IcHome, IcDoc } from '@/shared/components/ui/Icons'
import { formatARS } from '@/shared/lib/formatters'

function BottomNav({ active }: { active: 'home' | 'contrato' | 'documentos' }) {
  const navigate = useNavigate()
  const items = [
    { k: 'home', label: 'Inicio', to: '/inquilino' },
    { k: 'contrato', label: 'Contrato', to: '/inquilino/contrato' },
    { k: 'documentos', label: 'Documentos', to: '/inquilino/documentos' },
  ] as const
  return (
    <div style={{ position: 'fixed', bottom: 0, left: 0, right: 0, background: 'var(--surface)', borderTop: '1px solid var(--hairline)', display: 'flex', justifyContent: 'space-around', padding: '8px 0 16px', zIndex: 100 }}>
      {items.map((item) => (
        <button key={item.k} onClick={() => navigate(item.to)} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 4, background: 'none', border: 'none', cursor: 'pointer', color: active === item.k ? 'var(--brand)' : 'var(--muted)', fontFamily: 'inherit' }}>
          {item.k === 'home' ? <IcHome size={20} /> : item.k === 'contrato' ? <IcDoc size={20} /> : <IcShield size={20} />}
          <span style={{ fontSize: 10, fontWeight: 500 }}>{item.label}</span>
        </button>
      ))}
    </div>
  )
}

export default function TenantContratoPage() {
  return (
    <div style={{ maxWidth: 420, margin: '0 auto', padding: '20px 18px 80px', display: 'flex', flexDirection: 'column', gap: 14 }}>
      {/* Header */}
      <div>
        <div className="row" style={{ gap: 6 }}>
          <span className="chip chip--ok" style={{ height: 18, fontSize: 10 }}><span className="dot" />Vigente</span>
          <span style={{ fontSize: 11, color: 'var(--muted)' }}>C-1042</span>
        </div>
        <h2 style={{ fontSize: 20, fontWeight: 600, margin: '6px 0 2px', letterSpacing: '-.01em' }}>Tu contrato</h2>
        <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>15 jul 2024 — 15 jul 2027</div>
      </div>

      {/* Cálculo */}
      <div className="card" style={{ padding: 14 }}>
        <div className="between">
          <span className="sect-title" style={{ margin: 0 }}>Cómo se calcula</span>
          <span className="chip chip--icl" style={{ height: 18, fontSize: 10 }}><span className="dot" />ICL</span>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8, marginTop: 12 }}>
          <div style={{ padding: 10, background: 'var(--surface-2)', borderRadius: 8 }}>
            <div style={{ fontSize: 10, color: 'var(--muted)', textTransform: 'uppercase', letterSpacing: '.05em' }}>Base</div>
            <div style={{ fontSize: 16, fontWeight: 600, marginTop: 4, fontVariantNumeric: 'tabular-nums' }}>{formatARS(432000)}</div>
            <div style={{ fontSize: 10, color: 'var(--muted)', marginTop: 2 }}>15 abr</div>
          </div>
          <div style={{ padding: 10, background: 'var(--brand)', color: 'white', borderRadius: 8 }}>
            <div style={{ fontSize: 10, opacity: 0.8, textTransform: 'uppercase', letterSpacing: '.05em' }}>Nuevo (15 jul)</div>
            <div style={{ fontSize: 16, fontWeight: 600, marginTop: 4, fontVariantNumeric: 'tabular-nums' }}>{formatARS(485000)}</div>
            <div style={{ fontSize: 10, opacity: 0.8, marginTop: 2 }}>+12,4%</div>
          </div>
        </div>

        <div style={{ marginTop: 12, padding: 10, background: 'var(--surface-3)', borderRadius: 8, fontSize: 11, color: 'var(--ink-soft)', lineHeight: 1.5 }}>
          El alquiler se ajusta cada 3 meses según el <b>Índice ICL</b> que publica el BCRA. Coeficiente del próximo ajuste: <b className="mono">1,1227</b>.
        </div>

        <div className="row" style={{ marginTop: 10, gap: 6, fontSize: 11, color: 'var(--muted)' }}>
          <IcShield size={12} /> Datos verificados con BCRA
        </div>
      </div>

      {/* Datos del contrato */}
      <div className="card card-b" style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
        {[
          { k: 'Propiedad', v: 'Av. Córdoba 2840 · 7B' },
          { k: 'Propietario', v: 'Joaquín Ramírez' },
          { k: 'Día de pago', v: '5 de cada mes' },
          { k: 'Indexación', v: 'ICL · trimestral' },
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
            {formatARS(485000)}
          </div>
        </div>
        <div className="card" style={{ padding: '12px 14px', background: 'var(--brand-50)', borderColor: 'var(--brand-100)' }}>
          <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--brand-700)', textTransform: 'uppercase', letterSpacing: '.04em' }}>
            Próximo ajuste
          </div>
          <div style={{ fontSize: 18, fontWeight: 600, marginTop: 4, color: 'var(--brand-700)', fontVariantNumeric: 'tabular-nums' }}>
            +12,4%
          </div>
          <div style={{ fontSize: 10, color: 'var(--brand-700)', opacity: 0.8, marginTop: 2 }}>
            <IcArrowUp size={10} /> 15 jul 2026
          </div>
        </div>
      </div>

      {/* Acciones */}
      <button className="btn" style={{ width: '100%', justifyContent: 'center' }}>
        <IcDownload size={14} /> Descargar contrato
      </button>
      <button className="btn btn--ghost" style={{ width: '100%', justifyContent: 'center', color: 'var(--muted)' }}>
        <IcPhone size={14} /> Solicitar ayuda
      </button>

      <BottomNav active="contrato" />
    </div>
  )
}
