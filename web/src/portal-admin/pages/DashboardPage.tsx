import { useNavigate } from 'react-router'
import { AdminTopbar } from '../layouts/AdminTopbar'
import {
  IcDoc, IcAlert, IcCash, IcTrend, IcFilter, IcPlus,
  IcCalendar, IcChevDown, IcDownload, IcArrowUp, IcArrowDown,
  IcChev, IcShield, IcBell, Spark,
} from '@/shared/components/ui/Icons'
import { formatARS, formatDateShort } from '@/shared/lib/formatters'

const ICL_SERIES = [1.1218, 1.1542, 1.1981, 1.2358, 1.2794, 1.3210, 1.3680, 1.4115, 1.4602, 1.5104, 1.5621, 1.6210]
const IPC_SERIES = [3.8, 4.2, 4.6, 4.1, 3.7, 3.4, 3.1, 2.9, 2.7, 2.6, 2.5, 2.4]
const REVENUE_SERIES = [4.2, 4.4, 4.5, 4.7, 4.9, 5.1, 5.3, 5.4, 5.7, 5.9, 6.2, 6.5]
const OVERDUE_SERIES = [9, 12, 15, 18, 16, 14, 17, 15, 16, 14, 15, 14]
const ADJ_SERIES = [4, 5, 6, 8, 12, 18, 24, 28, 30, 31, 32, 32]

const UPCOMING_ADJS = [
  { c: 'C-1042', who: 'M. Álvarez', idx: 'ICL', d: '2026-05-15', b: 432000, n: 485000 },
  { c: 'C-1037', who: 'L. Gutiérrez', idx: 'ICL', d: '2026-05-15', b: 545000, n: 612000 },
  { c: 'C-1028', who: 'C. Ibáñez', idx: 'ICL', d: '2026-05-15', b: 481000, n: 540000 },
  { c: 'C-1039', who: 'F. Pereyra', idx: 'IPC', d: '2026-05-22', b: 312000, n: 320000 },
  { c: 'C-1031', who: 'R. Domínguez', idx: 'IPC', d: '2026-05-22', b: 400500, n: 410000 },
  { c: 'C-1024', who: 'S. Martínez', idx: 'IPC', d: '2026-05-22', b: 287900, n: 295000 },
]

const PENDING_ITEMS = [
  { icon: <IcAlert size={14} />, color: 'var(--danger)', text: '3 contratos vencidos > 7 días', sub: 'C-1024, C-1039, C-1009' },
  { icon: <IcTrend size={14} />, color: 'var(--icl)', text: 'Aplicar ajuste ICL trimestral', sub: '8 contratos · 15 may' },
  { icon: <IcDoc size={14} />, color: 'var(--brand)', text: 'Renovar contratos próximos a vencer', sub: '2 en 30 días' },
  { icon: <IcShield size={14} />, color: 'var(--warn)', text: 'Subir comprobantes faltantes', sub: 'abril · 4 contratos' },
]

export default function DashboardPage() {
  const navigate = useNavigate()

  const stats = [
    { lbl: 'Contratos vigentes', val: '247', delta: '+8', deltaUp: true, series: REVENUE_SERIES, icon: <IcDoc size={18} />, color: 'var(--brand)' },
    { lbl: 'Pagos vencidos', val: '14', delta: '-3', deltaUp: true, series: OVERDUE_SERIES, icon: <IcAlert size={18} />, color: 'var(--danger)' },
    { lbl: 'Ingresos del mes', val: '$ 84,2M', delta: '+6,4%', deltaUp: true, series: REVENUE_SERIES, icon: <IcCash size={18} />, color: 'var(--ok)' },
    { lbl: 'Próximos ajustes', val: '32', delta: 'esta semana', deltaUp: null, series: ADJ_SERIES, icon: <IcTrend size={18} />, color: 'var(--icl)' },
  ]

  return (
    <>
      <AdminTopbar
        crumbs={['Panel']}
        right={
          <button
            className="btn btn--primary btn--sm"
            onClick={() => navigate('/admin/contratos')}
          >
            <IcPlus size={12} /> Nuevo contrato
          </button>
        }
      />
      <div className="page">
        {/* Header */}
        <div className="page-h">
          <div>
            <h1>Buen día</h1>
            <div className="lead">Vista consolidada de la operación de hoy · 7 may 2026</div>
          </div>
          <div className="row">
            <button className="btn btn--sm">
              <IcCalendar size={12} /> Últimos 30 días <IcChevDown size={12} />
            </button>
            <button className="btn btn--sm">
              <IcDownload size={12} /> Exportar
            </button>
          </div>
        </div>

        {/* Stat cards */}
        <div className="grid-4">
          {stats.map((s, i) => (
            <div key={i} className="card stat">
              <div className="between">
                <span className="lbl">{s.lbl}</span>
                <span style={{ color: s.color, opacity: 0.8 }}>{s.icon}</span>
              </div>
              <div className="val">{s.val}</div>
              <div className="between" style={{ marginTop: 8 }}>
                {s.deltaUp === null
                  ? <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>{s.delta}</span>
                  : (
                    <span className={`delta-pill ${s.deltaUp ? 'up' : 'down'}`}>
                      {s.deltaUp ? <IcArrowUp size={10} /> : <IcArrowDown size={10} />}
                      {s.delta}
                    </span>
                  )
                }
                <Spark data={s.series} color={s.color} w={84} h={22} />
              </div>
            </div>
          ))}
        </div>

        {/* Two-column section */}
        <div style={{ display: 'grid', gridTemplateColumns: '1.4fr 1fr', gap: 'var(--s-7)' }}>
          {/* Próximos ajustes */}
          <div className="card">
            <div className="card-h">
              <div>
                <h3>Próximos ajustes</h3>
                <div className="sub">Próximos 30 días — datos del BCRA e INDEC</div>
              </div>
              <div className="row">
                <button className="btn btn--sm btn--ghost"><IcFilter size={12} /> Filtro</button>
                <button className="btn btn--sm" onClick={() => navigate('/admin/contratos')}>
                  Ver todos <IcChev size={12} />
                </button>
              </div>
            </div>
            <table className="tbl">
              <thead>
                <tr>
                  <th>Contrato</th>
                  <th>Índice</th>
                  <th>Fecha</th>
                  <th className="num">Base</th>
                  <th className="num">Nuevo</th>
                  <th className="num">Δ</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {UPCOMING_ADJS.map((r, i) => {
                  const pct = ((r.n - r.b) / r.b) * 100
                  return (
                    <tr key={i} style={{ cursor: 'pointer' }} onClick={() => navigate('/admin/contratos/C-1042')}>
                      <td>
                        <div style={{ fontWeight: 500 }}>{r.c}</div>
                        <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>{r.who}</div>
                      </td>
                      <td>
                        <span className={`chip chip--${r.idx === 'ICL' ? 'icl' : 'ipc'}`}>
                          <span className="dot" />{r.idx}
                        </span>
                      </td>
                      <td>{formatDateShort(r.d)}</td>
                      <td className="num">{formatARS(r.b)}</td>
                      <td className="num"><b>{formatARS(r.n)}</b></td>
                      <td className="num">
                        <span className="delta-pill up">+{pct.toFixed(1)}%</span>
                      </td>
                      <td><button className="btn btn--ghost btn--sm btn--icon"><IcChev size={12} /></button></td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          {/* Columna derecha */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--s-7)' }}>
            {/* Índices del mercado */}
            <div className="card">
              <div className="card-h">
                <h3>Índices del mercado</h3>
                <div className="sub">en vivo</div>
              </div>
              <div className="card-b" style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
                <div>
                  <div className="between">
                    <span className="chip chip--icl"><span className="dot" />ICL · trimestral</span>
                    <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>BCRA · 06 may</span>
                  </div>
                  <div className="between" style={{ marginTop: 6 }}>
                    <div style={{ fontSize: 22, fontWeight: 600, fontVariantNumeric: 'tabular-nums', letterSpacing: '-.01em' }}>
                      1,6210
                    </div>
                    <span className="delta-pill up"><IcArrowUp size={10} />+3,87%</span>
                  </div>
                  <Spark data={ICL_SERIES} color="var(--icl)" w={300} h={32} />
                </div>
                <div className="hr" style={{ margin: 0 }} />
                <div>
                  <div className="between">
                    <span className="chip chip--ipc"><span className="dot" />IPC · mensual</span>
                    <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>INDEC · abr 2026</span>
                  </div>
                  <div className="between" style={{ marginTop: 6 }}>
                    <div style={{ fontSize: 22, fontWeight: 600, fontVariantNumeric: 'tabular-nums', letterSpacing: '-.01em' }}>
                      2,4%
                    </div>
                    <span className="delta-pill up" style={{ background: 'var(--ok-50)', color: 'var(--ok)' }}>
                      <IcArrowDown size={10} />−0,1pp
                    </span>
                  </div>
                  <Spark data={IPC_SERIES} color="var(--ipc)" w={300} h={32} />
                </div>
              </div>
            </div>

            {/* Pendientes */}
            <div className="card">
              <div className="card-h">
                <h3>Pendientes</h3>
                <span className="chip chip--warn">5</span>
              </div>
              <div style={{ padding: '0 4px 4px' }}>
                {PENDING_ITEMS.map((r, i) => (
                  <div
                    key={i}
                    style={{
                      display: 'flex', gap: 12, padding: '10px 12px',
                      borderBottom: i < PENDING_ITEMS.length - 1 ? '1px solid var(--hairline)' : 'none',
                      alignItems: 'flex-start',
                    }}
                  >
                    <div
                      style={{
                        width: 28, height: 28, borderRadius: 7,
                        background: 'var(--surface-3)', color: r.color,
                        display: 'grid', placeItems: 'center', flex: '0 0 auto',
                      }}
                    >
                      {r.icon}
                    </div>
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <div style={{ fontSize: 'var(--fs-sm)', fontWeight: 500 }}>{r.text}</div>
                      <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', marginTop: 2 }}>{r.sub}</div>
                    </div>
                    <button className="btn btn--ghost btn--sm btn--icon"><IcChev size={12} /></button>
                  </div>
                ))}
              </div>
            </div>

            {/* Notificaciones enviadas */}
            <div className="card">
              <div className="card-h">
                <h3>Notificaciones</h3>
                <div className="sub">últimas 24h</div>
              </div>
              <div className="card-b">
                <div className="between">
                  <div className="row">
                    <IcBell size={14} style={{ color: 'var(--ok)' }} />
                    <span style={{ fontSize: 'var(--fs-sm)' }}>8 emails enviados</span>
                  </div>
                  <span className="chip chip--ok"><span className="dot" />Entregados</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  )
}
