import { useState } from 'react'
import { useNavigate } from 'react-router'
import {
  IcDownload, IcShield, IcDoc, IcTrend, IcLink, IcClock, IcHome, IcReceipt,
} from '@/shared/components/ui/Icons'
import { formatDate } from '@/shared/lib/formatters'

interface DocItem {
  n: string
  d: string
  k: 'receipt' | 'adj' | 'contract'
}

const DOCS: DocItem[] = [
  { n: 'Recibo abril 2026', d: '2026-04-05', k: 'receipt' },
  { n: 'Recibo marzo 2026', d: '2026-03-06', k: 'receipt' },
  { n: 'Comprobante ajuste 15-abr', d: '2026-04-15', k: 'adj' },
  { n: 'Recibo febrero 2026', d: '2026-02-05', k: 'receipt' },
  { n: 'Contrato firmado', d: '2024-07-15', k: 'contract' },
]

function DocIcon({ kind }: { kind: DocItem['k'] }) {
  if (kind === 'receipt') return <IcReceipt size={16} />
  if (kind === 'adj') return <IcTrend size={16} />
  return <IcDoc size={16} />
}

function docColors(k: DocItem['k']): { bg: string; fg: string } {
  if (k === 'receipt') return { bg: 'var(--ok-50)', fg: 'var(--ok)' }
  if (k === 'adj') return { bg: 'var(--icl-50)', fg: 'var(--icl)' }
  return { bg: 'var(--brand-50)', fg: 'var(--brand)' }
}

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

export default function TenantDocumentosPage() {
  const [showLink, setShowLink] = useState(false)

  return (
    <div style={{ maxWidth: 420, margin: '0 auto', padding: '20px 18px 80px', display: 'flex', flexDirection: 'column', gap: 14, position: 'relative' }}>
      {/* Header */}
      <div>
        <h2 style={{ fontSize: 20, fontWeight: 600, margin: 0, letterSpacing: '-.01em' }}>Mis documentos</h2>
        <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', marginTop: 2 }}>
          Descargas seguras · links válidos por 5 minutos
        </div>
      </div>

      {/* Lista de documentos */}
      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        {DOCS.map((d, i) => {
          const colors = docColors(d.k)
          return (
            <div
              key={i}
              className="between"
              style={{
                padding: '12px 14px',
                borderBottom: i < DOCS.length - 1 ? '1px solid var(--hairline)' : 'none',
                cursor: 'pointer',
              }}
              onClick={() => {
                if (i === 0) setShowLink(true)
              }}
            >
              <div className="row" style={{ gap: 10 }}>
                <div style={{ width: 34, height: 34, borderRadius: 8, background: colors.bg, color: colors.fg, display: 'grid', placeItems: 'center', flex: '0 0 auto' }}>
                  <DocIcon kind={d.k} />
                </div>
                <div style={{ minWidth: 0 }}>
                  <div style={{ fontSize: 'var(--fs-sm)', fontWeight: 500 }}>{d.n}</div>
                  <div style={{ fontSize: 11, color: 'var(--muted)' }}>{formatDate(d.d)}</div>
                </div>
              </div>
              <button
                className="btn btn--ghost btn--sm btn--icon"
                onClick={(e) => { e.stopPropagation(); setShowLink(true) }}
              >
                <IcDownload size={14} />
              </button>
            </div>
          )
        })}
      </div>

      {/* Bottom sheet: secure link */}
      {showLink && (
        <div
          onClick={() => setShowLink(false)}
          style={{
            position: 'fixed', inset: 0,
            background: 'rgba(20,20,16,.4)',
            display: 'flex', alignItems: 'flex-end',
            zIndex: 200,
          }}
        >
          <div
            onClick={(e) => e.stopPropagation()}
            className="card"
            style={{
              width: '100%', maxWidth: 420, margin: '0 auto',
              borderRadius: '16px 16px 0 0', padding: 18,
              border: 'none', boxShadow: '0 -10px 30px rgba(0,0,0,.18)',
            }}
          >
            <div style={{ width: 36, height: 4, background: 'var(--n-150)', borderRadius: 2, margin: '-2px auto 14px' }} />

            <div className="row" style={{ marginBottom: 10 }}>
              <div style={{ width: 36, height: 36, borderRadius: 9, background: 'var(--brand-50)', color: 'var(--brand)', display: 'grid', placeItems: 'center' }}>
                <IcShield size={18} />
              </div>
              <div>
                <div style={{ fontSize: 'var(--fs-sm)', fontWeight: 600 }}>Link seguro</div>
                <div style={{ fontSize: 11, color: 'var(--muted)' }}>El link expira en 5 minutos</div>
              </div>
            </div>

            <div style={{
              background: 'var(--surface-2)', border: '1px solid var(--hairline)',
              borderRadius: 8, padding: '10px 12px',
              fontFamily: 'var(--font-mono)', fontSize: 11, color: 'var(--ink-soft)',
              wordBreak: 'break-all', lineHeight: 1.5,
            }}>
              alquilar.io/d/r-2026-04?token=eyJhbGciOiJIUzI1NiI…
            </div>

            <div className="between" style={{ marginTop: 8, fontSize: 11 }}>
              <span style={{ color: 'var(--muted)', display: 'inline-flex', alignItems: 'center', gap: 4 }}>
                <IcClock size={12} /> Expira en
              </span>
              <span className="mono" style={{ color: 'var(--warn)', fontWeight: 600 }}>04:42</span>
            </div>

            <div className="row" style={{ marginTop: 14, gap: 6 }}>
              <button className="btn" style={{ flex: 1, justifyContent: 'center' }}>
                <IcLink size={14} /> Copiar
              </button>
              <button className="btn btn--primary" style={{ flex: 1, justifyContent: 'center' }}>
                <IcDownload size={14} /> Descargar
              </button>
            </div>
          </div>
        </div>
      )}

      <BottomNav active="documentos" />
    </div>
  )
}
