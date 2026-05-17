import { useState } from 'react'
import { useParams } from 'react-router'
import { AdminTopbar } from '../layouts/AdminTopbar'
import {
  IcEdit, IcEllipsis, IcPin, IcArrowUp,
  IcShield, IcBell, IcDownload, IcUpload, IcPlus,
  IcCalendar, IcDoc, IcTrend, IcClock, IcChev,
} from '@/shared/components/ui/Icons'
import { formatARS, formatDate } from '@/shared/lib/formatters'

type TabKey = 'overview' | 'payments' | 'adjustments' | 'documents'

const TABS = [
  { k: 'overview' as TabKey, lbl: 'Resumen' },
  { k: 'payments' as TabKey, lbl: 'Historial de pagos' },
  { k: 'adjustments' as TabKey, lbl: 'Historial de ajustes' },
  { k: 'documents' as TabKey, lbl: 'Documentos' },
]

function CalcCell({
  k, v, sub, accent, solid,
}: {
  k: string; v: string; sub: React.ReactNode; accent?: string; solid?: boolean
}) {
  return (
    <div
      style={{
        padding: '16px 18px',
        borderRadius: 'var(--r-4)',
        background: solid ? 'var(--brand)' : 'var(--surface-2)',
        color: solid ? 'var(--on-brand)' : 'var(--ink)',
        border: solid ? 'none' : '1px solid var(--hairline)',
        textAlign: 'center',
      }}
    >
      <div style={{ fontSize: 'var(--fs-xs)', textTransform: 'uppercase', letterSpacing: '.05em', color: solid ? 'rgba(255,255,255,.75)' : 'var(--muted)', fontWeight: 500 }}>{k}</div>
      <div style={{ fontSize: 24, fontWeight: 600, letterSpacing: '-.02em', fontVariantNumeric: 'tabular-nums', marginTop: 4, color: accent ?? 'inherit' }}>{v}</div>
      <div style={{ fontSize: 'var(--fs-xs)', color: solid ? 'rgba(255,255,255,.75)' : 'var(--muted)', marginTop: 4 }}>{sub}</div>
    </div>
  )
}

function OverviewTab() {
  const TIMELINE_EVENTS = [
    { d: '15 jul 2024', t: 'Contrato firmado', s: 'Alta inicial · alquiler $ 280.000', icon: <IcDoc size={11} />, color: 'var(--ink)', future: false, note: null },
    { d: '15 oct 2024', t: 'Ajuste ICL aplicado', s: '+9,2% → $ 305.760', icon: <IcTrend size={11} />, color: 'var(--icl)', future: false, note: null },
    { d: '15 ene 2025', t: 'Ajuste ICL aplicado', s: '+11,1% → $ 339.700', icon: <IcTrend size={11} />, color: 'var(--icl)', future: false, note: null },
    { d: '15 abr 2025', t: 'Ajuste manual', s: 'Acuerdo entre partes · +6,5% → $ 361.780', icon: <IcEdit size={11} />, color: 'var(--warn)', future: false, note: 'Nota: descuento por mejoras del 0,5%' },
    { d: '15 jul 2025', t: 'Ajuste ICL aplicado', s: '+9,9% → $ 397.580', icon: <IcTrend size={11} />, color: 'var(--icl)', future: false, note: null },
    { d: '15 abr 2026', t: 'Ajuste ICL aplicado', s: '+8,7% → $ 432.000', icon: <IcTrend size={11} />, color: 'var(--icl)', future: false, note: null },
    { d: '15 jul 2026', t: 'Próximo ajuste ICL', s: '+12,3% → $ 485.000 (proy.)', icon: <IcClock size={11} />, color: 'var(--brand)', future: true, note: null },
  ]

  return (
    <>
      <div style={{ display: 'grid', gridTemplateColumns: '1.5fr 1fr', gap: 'var(--s-7)' }}>
        {/* Cálculo próximo ajuste */}
        <div className="card">
          <div className="card-h">
            <div>
              <h3>Próximo ajuste — cómo se calcula</h3>
              <div className="sub">15 jul 2026 · ICL trimestral · auto-aplicable</div>
            </div>
            <div className="row">
              <button className="btn btn--sm"><IcEdit size={12} /> Ajuste manual</button>
              <button className="btn btn--sm btn--primary">Aplicar ahora <IcChev size={12} /></button>
            </div>
          </div>
          <div className="card-b" style={{ display: 'flex', flexDirection: 'column', gap: 18 }}>
            {/* Fórmula visual */}
            <div style={{ display: 'grid', gridTemplateColumns: '1fr auto 1fr auto 1fr', alignItems: 'center', gap: 12 }}>
              <CalcCell k="Base" v="$ 432.000" sub="15 abr 2026" />
              <div style={{ fontSize: 22, color: 'var(--muted)', textAlign: 'center', fontWeight: 300 }}>×</div>
              <CalcCell k="Coeficiente" v="1,1227" sub="ICL final / ICL base" accent="var(--icl)" />
              <div style={{ fontSize: 22, color: 'var(--muted)', textAlign: 'center', fontWeight: 300 }}>=</div>
              <CalcCell
                k="Alquiler proyectado"
                v="$ 485.000"
                sub={<span className="delta-pill up"><IcArrowUp size={10} />+12,27%</span>}
                solid
              />
            </div>

            {/* Detalles coeficiente */}
            <div style={{ background: 'var(--surface-2)', borderRadius: 'var(--r-3)', padding: '12px 14px', display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 12 }}>
              {[
                { k: 'ICL 15-abr-2026', v: '1,4456', tag: 'base' },
                { k: 'ICL 15-jul-2026', v: '1,6224', tag: 'end' },
                { k: 'Coeficiente', v: '1,12230', tag: 'result' },
              ].map((d) => {
                const tagBg = d.tag === 'result' ? 'var(--brand)' : 'var(--n-150)'
                const tagFg = d.tag === 'result' ? 'var(--on-brand)' : 'var(--ink-soft)'
                return (
                  <div key={d.k}>
                    <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', textTransform: 'uppercase', letterSpacing: '.04em', fontWeight: 500, display: 'flex', gap: 6, alignItems: 'center' }}>
                      <span style={{ background: tagBg, color: tagFg, fontSize: 9, padding: '1px 5px', borderRadius: 3, textTransform: 'uppercase', fontWeight: 600 }}>{d.tag}</span>
                      {d.k}
                    </div>
                    <div className="mono" style={{ fontSize: 18, fontWeight: 500, marginTop: 4, fontVariantNumeric: 'tabular-nums' }}>{d.v}</div>
                  </div>
                )
              })}
            </div>

            <div className="between" style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>
              <span style={{ display: 'inline-flex', gap: 6, alignItems: 'center' }}>
                <IcShield size={12} /> Datos verificados con BCRA · sincronizado hoy 09:14
              </span>
              <span style={{ display: 'inline-flex', gap: 6, alignItems: 'center' }}>
                <IcBell size={12} /> Inquilina será notificada por email
              </span>
            </div>
          </div>
        </div>

        {/* Partes y términos */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--s-7)' }}>
          <div className="card card-b" style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
            <div className="sect-title">Partes</div>
            {[
              { name: 'Marcela Álvarez', role: 'Inquilina', sub: 'm.alvarez@gmail.com', tag: 'active' },
              { name: 'Joaquín Ramírez', role: 'Propietario', sub: '+54 11 5510-2933', tag: null },
            ].map((p) => {
              const initials = p.name.split(' ').map((s) => s[0]).slice(0, 2).join('')
              return (
                <div key={p.name} className="row" style={{ gap: 10 }}>
                  <div className="mono-avatar">{initials}</div>
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div className="row" style={{ gap: 6 }}>
                      <span style={{ fontSize: 'var(--fs-sm)', fontWeight: 500 }}>{p.name}</span>
                      <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>· {p.role}</span>
                    </div>
                    <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>{p.sub}</div>
                  </div>
                </div>
              )
            })}
            <div className="hr" style={{ margin: 0 }} />
            <div className="sect-title">Términos</div>
            {[
              { k: 'Plazo', v: '36 meses' },
              { k: 'Depósito', v: '$ 432.000 (1 mes)' },
              { k: 'Indexación', v: 'ICL · trimestral' },
              { k: 'Punitorio', v: '0,15% / día' },
              { k: 'Día de pago', v: 'día 5' },
            ].map((kv) => (
              <div key={kv.k} className="between">
                <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>{kv.k}</span>
                <span style={{ fontSize: 'var(--fs-sm)', fontWeight: 500 }}>{kv.v}</span>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Línea de tiempo */}
      <div className="card" style={{ marginTop: 'var(--s-7)' }}>
        <div className="card-h">
          <h3>Línea de tiempo</h3>
          <div className="sub">Eventos clave del contrato</div>
        </div>
        <div className="card-b">
          <div style={{ position: 'relative', paddingLeft: 22 }}>
            <div style={{ position: 'absolute', left: 9, top: 6, bottom: 6, width: 2, background: 'var(--hairline)' }} />
            {TIMELINE_EVENTS.map((e, i) => (
              <div key={i} style={{ position: 'relative', paddingBottom: i < TIMELINE_EVENTS.length - 1 ? 18 : 0 }}>
                <div style={{
                  position: 'absolute', left: -22, top: 1, width: 20, height: 20, borderRadius: 50,
                  background: e.future ? 'var(--surface)' : e.color, color: 'white',
                  border: e.future ? `2px dashed ${e.color}` : 'none',
                  display: 'grid', placeItems: 'center',
                }}>
                  {!e.future && e.icon}
                </div>
                <div className="between" style={{ alignItems: 'flex-start' }}>
                  <div>
                    <div className="row" style={{ gap: 8 }}>
                      <span style={{ fontSize: 'var(--fs-sm)', fontWeight: 500 }}>{e.t}</span>
                      {e.future && <span className="chip chip--info"><span className="dot" />programado</span>}
                    </div>
                    <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', marginTop: 2 }}>{e.s}</div>
                    {e.note && (
                      <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--warn)', marginTop: 4, padding: '4px 8px', background: 'var(--warn-50)', borderRadius: 4, display: 'inline-block' }}>
                        {e.note}
                      </div>
                    )}
                  </div>
                  <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', whiteSpace: 'nowrap', fontVariantNumeric: 'tabular-nums' }}>{e.d}</div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </>
  )
}

function PaymentsTab() {
  const rows = [
    { d: '2026-05-05', per: 'May 2026', amt: 485000, met: 'Transferencia', st: 'paid' as const, n: 'TRX-29481', note: null },
    { d: '2026-04-05', per: 'Abr 2026', amt: 485000, met: 'Transferencia', st: 'paid' as const, n: 'TRX-29102', note: null },
    { d: '2026-03-06', per: 'Mar 2026', amt: 432000, met: 'Transferencia', st: 'paid' as const, n: 'TRX-28874', note: 'Pagado con 1 día de retraso' },
    { d: '2026-02-05', per: 'Feb 2026', amt: 432000, met: 'Mercado Pago', st: 'paid' as const, n: 'TRX-28490', note: null },
    { d: '2026-01-05', per: 'Ene 2026', amt: 432000, met: 'Transferencia', st: 'paid' as const, n: 'TRX-28110', note: null },
    { d: '2025-12-05', per: 'Dic 2025', amt: 397580, met: 'Mercado Pago', st: 'partial' as const, n: 'TRX-27890', note: 'Pagó $ 350.000 · saldo cubierto en enero' },
  ]

  const STATUS_MAP = {
    paid: { cls: 'chip--ok', lbl: 'Pagado' },
    partial: { cls: 'chip--info', lbl: 'Parcial' },
    late: { cls: 'chip--danger', lbl: 'Vencido' },
    pending: { cls: 'chip--warn', lbl: 'Pendiente' },
  }

  return (
    <>
      <div className="between" style={{ marginBottom: 'var(--s-7)' }}>
        <div className="row">
          <button className="chip chip--solid">Todos</button>
          <button className="chip">Pagado</button>
          <button className="chip">Pendiente</button>
          <button className="chip">Vencido</button>
        </div>
        <div className="row">
          <button className="btn btn--sm"><IcCalendar size={12} /> 2026</button>
          <button className="btn btn--sm"><IcDownload size={12} /> Exportar</button>
          <button className="btn btn--sm btn--primary"><IcPlus size={12} /> Registrar pago</button>
        </div>
      </div>
      <div className="card" style={{ overflow: 'hidden' }}>
        <table className="tbl">
          <thead>
            <tr>
              <th>Período</th>
              <th>Fecha</th>
              <th className="num">Importe</th>
              <th>Medio</th>
              <th>Comprobante</th>
              <th>Estado</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r, i) => (
              <tr key={i}>
                <td>
                  <b>{r.per}</b>
                  {r.note && <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--warn)', marginTop: 2 }}>· {r.note}</div>}
                </td>
                <td className="muted">{formatDate(r.d)}</td>
                <td className="num"><b>{formatARS(r.amt)}</b></td>
                <td>{r.met}</td>
                <td><span className="mono" style={{ color: 'var(--muted)', fontSize: 'var(--fs-xs)' }}>{r.n}</span></td>
                <td>
                  <span className={`chip ${STATUS_MAP[r.st].cls}`}>
                    <span className="dot" />{STATUS_MAP[r.st].lbl}
                  </span>
                </td>
                <td>
                  <div className="row" style={{ gap: 2 }}>
                    <button className="btn btn--ghost btn--sm btn--icon"><IcDownload size={12} /></button>
                    <button className="btn btn--ghost btn--sm btn--icon"><IcEllipsis size={12} /></button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}

function AdjustmentsTab() {
  const adjs = [
    { d: '2026-04-15', idx: 'ICL', base: 397580, val: 432000, coef: '1,0866', trig: 'auto', note: null },
    { d: '2026-01-15', idx: 'ICL', base: 361780, val: 397580, coef: '1,0989', trig: 'auto', note: null },
    { d: '2025-10-15', idx: 'ICL', base: 339700, val: 361780, coef: '1,0650', trig: 'manual', note: 'Acuerdo entre partes · descuento de 0,5pp por mejoras edilicias' },
    { d: '2025-07-15', idx: 'ICL', base: 305760, val: 339700, coef: '1,1110', trig: 'auto', note: null },
    { d: '2025-04-15', idx: 'ICL', base: 280000, val: 305760, coef: '1,0920', trig: 'auto', note: null },
  ]

  return (
    <>
      <div className="grid-3" style={{ marginBottom: 'var(--s-7)' }}>
        <div className="card stat">
          <span className="lbl">Acumulado en el contrato</span>
          <div className="val">+73,2%</div>
          <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', marginTop: 4 }}>5 ajustes desde 15 jul 2024</div>
        </div>
        <div className="card stat">
          <span className="lbl">Promedio trimestral</span>
          <div className="val">+9,7%</div>
          <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', marginTop: 4 }}>vs ICL del mercado +10,3%</div>
        </div>
        <div className="card stat">
          <span className="lbl">Próximo ajuste</span>
          <div className="val" style={{ color: 'var(--brand)' }}>+12,4%</div>
          <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', marginTop: 4 }}>15 jul 2026 · auto-aplicable</div>
        </div>
      </div>

      <div className="card">
        <div className="card-h">
          <h3>Historial de ajustes</h3>
          <div className="sub">Click para ver el cálculo completo</div>
        </div>
        <table className="tbl">
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Índice</th>
              <th>Coeficiente</th>
              <th className="num">Base</th>
              <th className="num">Nuevo valor</th>
              <th className="num">Δ</th>
              <th>Origen</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {adjs.map((a, i) => {
              const pct = ((a.val - a.base) / a.base) * 100
              return (
                <tr key={i}>
                  <td><b>{formatDate(a.d)}</b></td>
                  <td>
                    <span className={`chip chip--${a.idx === 'ICL' ? 'icl' : 'ipc'}`}>
                      <span className="dot" />{a.idx}
                    </span>
                  </td>
                  <td className="mono tnum">{a.coef}</td>
                  <td className="num">{formatARS(a.base)}</td>
                  <td className="num"><b>{formatARS(a.val)}</b></td>
                  <td className="num">
                    <span className="delta-pill up"><IcArrowUp size={10} />+{pct.toFixed(2)}%</span>
                  </td>
                  <td>
                    {a.trig === 'auto'
                      ? <span className="chip"><span className="dot" style={{ background: 'var(--ok)' }} />Automático</span>
                      : <span className="chip chip--warn"><span className="dot" />Manual</span>
                    }
                    {a.note && (
                      <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--warn)', marginTop: 4, maxWidth: 280, lineHeight: 1.35 }}>
                        ↳ {a.note}
                      </div>
                    )}
                  </td>
                  <td><button className="btn btn--ghost btn--sm btn--icon"><IcChev size={12} /></button></td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </>
  )
}

function DocumentsTab() {
  const docs = [
    { n: 'Contrato firmado', f: 'contrato-c1042.pdf', s: '2,4 MB', d: '2024-07-15', k: 'contract' },
    { n: 'DNI inquilina (frente)', f: 'dni-alvarez-f.jpg', s: '640 KB', d: '2024-07-12', k: 'id' },
    { n: 'DNI inquilina (dorso)', f: 'dni-alvarez-d.jpg', s: '612 KB', d: '2024-07-12', k: 'id' },
    { n: 'Garantía propietaria', f: 'garantia.pdf', s: '1,1 MB', d: '2024-07-13', k: 'guarantee' },
    { n: 'Recibo abril 2026', f: 'recibo-2026-04.pdf', s: '180 KB', d: '2026-04-05', k: 'receipt' },
    { n: 'Recibo marzo 2026', f: 'recibo-2026-03.pdf', s: '180 KB', d: '2026-03-06', k: 'receipt' },
  ]

  return (
    <>
      <div className="between" style={{ marginBottom: 'var(--s-7)' }}>
        <div className="row">
          <button className="chip chip--solid">Todos · {docs.length}</button>
          <button className="chip">Contrato</button>
          <button className="chip">Recibos</button>
          <button className="chip">Identidad</button>
        </div>
        <button className="btn btn--sm btn--primary"><IcUpload size={12} /> Subir</button>
      </div>

      <div className="grid-3">
        {docs.map((d, i) => (
          <div key={i} className="card" style={{ padding: 14, display: 'flex', flexDirection: 'column', gap: 12 }}>
            <div className="row" style={{ alignItems: 'flex-start' }}>
              <div style={{
                width: 36, height: 44, borderRadius: 4,
                background: 'var(--surface-2)', border: '1px solid var(--hairline)',
                display: 'grid', placeItems: 'center', color: 'var(--brand)', flex: '0 0 auto',
              }}>
                <IcDoc size={18} />
              </div>
              <div style={{ flex: 1, minWidth: 0 }}>
                <div style={{ fontSize: 'var(--fs-sm)', fontWeight: 500, lineHeight: 1.3 }}>{d.n}</div>
                <div className="mono" style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', marginTop: 2 }}>{d.f}</div>
                <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', marginTop: 2 }}>
                  {d.s} · {formatDate(d.d)}
                </div>
              </div>
              <button className="btn btn--ghost btn--sm btn--icon"><IcEllipsis size={12} /></button>
            </div>
            <div style={{ background: 'var(--brand-50)', borderRadius: 6, padding: '8px 10px', display: 'flex', alignItems: 'center', gap: 8 }}>
              <IcShield size={14} style={{ color: 'var(--brand)', flex: '0 0 auto' }} />
              <span style={{ fontSize: 11, color: 'var(--brand-700)', flex: 1 }}>El link expira en 5 minutos</span>
              <button className="btn btn--sm" style={{ background: 'var(--brand)', color: 'var(--on-brand)', border: 'none', height: 24, fontSize: 11 }}>
                <IcDownload size={11} /> Descargar
              </button>
            </div>
          </div>
        ))}
      </div>
    </>
  )
}

export default function ContratoDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [tab, setTab] = useState<TabKey>('overview')

  const contratoId = id ?? 'C-1042'

  return (
    <>
      <AdminTopbar
        crumbs={['Contratos', contratoId]}
        right={
          <div className="row">
            <button className="btn btn--sm"><IcEdit size={12} /> Editar</button>
            <button className="btn btn--sm btn--icon"><IcEllipsis size={12} /></button>
          </div>
        }
      />
      <div className="page" style={{ padding: 0, gap: 0 }}>
        {/* Header del contrato */}
        <div style={{ padding: 'var(--s-9) var(--s-9) var(--s-7)', background: 'var(--surface)', borderBottom: '1px solid var(--hairline)' }}>
          <div className="between" style={{ alignItems: 'flex-start' }}>
            <div>
              <div className="row" style={{ marginBottom: 6 }}>
                <span className="chip chip--ok"><span className="dot" />Vigente</span>
                <span className="chip chip--icl"><span className="dot" />ICL · trim.</span>
                <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>Inicio 15 jul 2024 · Fin 15 jul 2027</span>
              </div>
              <h1 style={{ margin: 0, fontSize: 26, fontWeight: 600, letterSpacing: '-.02em' }}>
                {contratoId} · M. Álvarez
              </h1>
              <div className="row muted" style={{ marginTop: 4, fontSize: 'var(--fs-sm)' }}>
                <IcPin size={14} /> Av. Córdoba 2840 · 7B, CABA · Propietario: J. Ramírez
              </div>
            </div>
            <div className="row" style={{ alignItems: 'stretch' }}>
              <div className="card" style={{ padding: '10px 14px', display: 'flex', flexDirection: 'column', gap: 2 }}>
                <span className="label">Alquiler vigente</span>
                <div style={{ fontSize: 20, fontWeight: 600, fontVariantNumeric: 'tabular-nums', letterSpacing: '-.01em' }}>
                  {formatARS(485000)}
                </div>
              </div>
              <div className="card" style={{ padding: '10px 14px', display: 'flex', flexDirection: 'column', gap: 2, background: 'var(--brand-50)', borderColor: 'var(--brand-100)' }}>
                <span className="label" style={{ color: 'var(--brand-700)' }}>Próximo ajuste</span>
                <div style={{ fontSize: 20, fontWeight: 600, fontVariantNumeric: 'tabular-nums', letterSpacing: '-.01em', color: 'var(--brand-700)' }}>
                  15 jul · +12,4%
                </div>
              </div>
            </div>
          </div>

          {/* Tabs */}
          <div style={{ marginTop: 18, display: 'flex', gap: 4 }}>
            {TABS.map((t) => (
              <button
                key={t.k}
                onClick={() => setTab(t.k)}
                style={{
                  padding: '8px 12px', borderRadius: 6, border: 'none',
                  background: tab === t.k ? 'var(--surface-3)' : 'transparent',
                  color: tab === t.k ? 'var(--ink)' : 'var(--muted)',
                  fontWeight: 500, fontSize: 'var(--fs-sm)', cursor: 'pointer',
                  fontFamily: 'inherit',
                }}
              >
                {t.lbl}
              </button>
            ))}
          </div>
        </div>

        <div className="page" style={{ padding: 'var(--s-9)', flex: 1 }}>
          {tab === 'overview' && <OverviewTab />}
          {tab === 'payments' && <PaymentsTab />}
          {tab === 'adjustments' && <AdjustmentsTab />}
          {tab === 'documents' && <DocumentsTab />}
        </div>
      </div>
    </>
  )
}
