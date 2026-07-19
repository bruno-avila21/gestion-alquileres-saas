import { useNavigate } from 'react-router'
import { AdminTopbar } from '../layouts/AdminTopbar'
import {
  IcDoc, IcAlert, IcCash, IcTrend, IcPlus,
  IcCalendar, IcChevDown, IcDownload, IcArrowUp,
  IcChev, IcShield, IcBell, Spark,
} from '@/shared/components/ui/Icons'
import { formatARS, formatDate } from '@/shared/lib/formatters'
import { downloadCsv } from '@/shared/lib/exportCsv'
import { useDashboard } from '@/features/dashboard/hooks/useDashboard'

const TX_LABEL = { Payment: 'Pago', RentCharge: 'Cargo', ManualDebit: 'Débito', ManualCredit: 'Crédito' } as const

export default function DashboardPage() {
  const navigate = useNavigate()
  const { data: dashboard, isLoading } = useDashboard()

  const activeContracts = dashboard?.activeContractsCount ?? 0
  const monthlyRevenue = dashboard?.monthlyRevenue ?? 0
  const expiring = dashboard?.expiringIn30DaysCount ?? 0
  const recentTx = dashboard?.recentTransactions ?? []

  const hour = new Date().getHours()
  const greeting = hour < 12 ? 'Buen día' : hour < 20 ? 'Buenas tardes' : 'Buenas noches'

  function handleExport() {
    downloadCsv(
      'transacciones-recientes.csv',
      ['Tipo', 'Período', 'Importe', 'Moneda', 'Estado'],
      recentTx.map((t) => [TX_LABEL[t.type], t.period, t.amount, t.currency, t.status]),
    )
  }

  const REVENUE_SERIES = [4.2, 4.4, 4.5, 4.7, 4.9, 5.1, 5.3, 5.4, 5.7, 5.9, 6.2, activeContracts || 6.5]
  const OVERDUE_SERIES = [9, 12, 15, 18, 16, 14, 17, 15, 16, 14, 15, expiring || 0]

  const stats = [
    {
      lbl: 'Contratos vigentes',
      val: isLoading ? '…' : String(activeContracts),
      delta: null,
      series: REVENUE_SERIES,
      icon: <IcDoc size={18} />,
      color: 'var(--brand)',
      to: '/admin/contratos',
    },
    {
      lbl: 'Vencen en 30 días',
      val: isLoading ? '…' : String(expiring),
      delta: null,
      series: OVERDUE_SERIES,
      icon: <IcAlert size={18} />,
      color: 'var(--danger)',
      to: '/admin/contratos',
    },
    {
      lbl: 'Ingresos mensuales',
      val: isLoading ? '…' : formatARS(monthlyRevenue),
      delta: null,
      series: REVENUE_SERIES,
      icon: <IcCash size={18} />,
      color: 'var(--ok)',
      to: '/admin/pagos',
    },
    {
      lbl: 'Trans. recientes',
      val: isLoading ? '…' : String(recentTx.length),
      delta: null,
      series: REVENUE_SERIES,
      icon: <IcTrend size={18} />,
      color: 'var(--icl)',
      to: '/admin/pagos',
    },
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
        <div className="page-h">
          <div>
            <h1>{greeting}</h1>
            <div className="lead">Vista consolidada de la operación</div>
          </div>
          <div className="row">
            <button className="btn btn--sm" disabled title="Filtro por fecha (próximamente)">
              <IcCalendar size={12} /> Hoy <IcChevDown size={12} />
            </button>
            <button className="btn btn--sm" onClick={handleExport} disabled={recentTx.length === 0}>
              <IcDownload size={12} /> Exportar
            </button>
          </div>
        </div>

        {/* Stat cards */}
        <div className="grid-4">
          {stats.map((s, i) => (
            <div key={i} className="card stat" style={{ cursor: 'pointer' }} onClick={() => navigate(s.to)}>
              <div className="between">
                <span className="lbl">{s.lbl}</span>
                <span style={{ color: s.color, opacity: 0.8 }}>{s.icon}</span>
              </div>
              <div className="val">{s.val}</div>
              <div className="between" style={{ marginTop: 8 }}>
                <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>tendencia (ilustrativa)</span>
                <Spark data={s.series} color={s.color} w={84} h={22} />
              </div>
            </div>
          ))}
        </div>

        {/* Two-column section */}
        <div style={{ display: 'grid', gridTemplateColumns: '1.4fr 1fr', gap: 'var(--s-7)' }}>
          {/* Transacciones recientes */}
          <div className="card">
            <div className="card-h">
              <div>
                <h3>Transacciones recientes</h3>
                <div className="sub">Últimas 5 de toda la organización</div>
              </div>
              <button className="btn btn--sm" onClick={() => navigate('/admin/contratos')}>
                Ver contratos <IcChev size={12} />
              </button>
            </div>
            {isLoading ? (
              <div style={{ padding: 24, textAlign: 'center', color: 'var(--muted)' }}>Cargando…</div>
            ) : recentTx.length === 0 ? (
              <div style={{ padding: 24, textAlign: 'center', color: 'var(--muted)' }}>Sin transacciones aún</div>
            ) : (
              <table className="tbl">
                <thead>
                  <tr>
                    <th>Tipo</th>
                    <th>Período</th>
                    <th className="num">Importe</th>
                    <th>Moneda</th>
                    <th>Fecha</th>
                  </tr>
                </thead>
                <tbody>
                  {recentTx.map((t) => (
                    <tr key={t.id}>
                      <td>
                        <span className={`chip ${t.type === 'Payment' ? 'chip--ok' : t.type === 'RentCharge' ? '' : 'chip--warn'}`}>
                          <span className="dot" />
                          {t.type === 'Payment' ? 'Pago' : t.type === 'RentCharge' ? 'Cargo' : t.type === 'ManualDebit' ? 'Débito' : 'Crédito'}
                        </span>
                      </td>
                      <td>{formatDate(t.period)}</td>
                      <td className="num"><b>{formatARS(t.amount)}</b></td>
                      <td>{t.currency}</td>
                      <td className="muted" style={{ fontSize: 'var(--fs-xs)' }}>{formatDate(t.createdAt.split('T')[0])}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>

          {/* Columna derecha */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--s-7)' }}>
            {/* Resumen de cartera */}
            <div className="card">
              <div className="card-h">
                <h3>Cartera</h3>
                <div className="sub">en vivo</div>
              </div>
              <div className="card-b" style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
                {[
                  { lbl: 'Contratos activos', val: String(activeContracts), color: 'var(--brand)' },
                  { lbl: 'Ingreso mensual total', val: formatARS(monthlyRevenue), color: 'var(--ok)' },
                  { lbl: 'Vencen en 30 días', val: String(expiring), color: expiring > 0 ? 'var(--danger)' : 'var(--muted)' },
                ].map((r) => (
                  <div key={r.lbl} className="between">
                    <span style={{ fontSize: 'var(--fs-sm)', color: 'var(--muted)' }}>{r.lbl}</span>
                    <span style={{ fontSize: 'var(--fs-sm)', fontWeight: 600, color: r.color }}>{isLoading ? '…' : r.val}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Acceso rápido */}
            <div className="card">
              <div className="card-h">
                <h3>Acceso rápido</h3>
              </div>
              <div style={{ padding: '0 4px 4px' }}>
                {[
                  { icon: <IcDoc size={14} />, color: 'var(--brand)', text: 'Ver contratos', to: '/admin/contratos' },
                  { icon: <IcTrend size={14} />, color: 'var(--icl)', text: 'Gestionar inquilinos', to: '/admin/inquilinos' },
                  { icon: <IcShield size={14} />, color: 'var(--warn)', text: 'Ver índices BCRA/INDEC', to: '/admin/indices' },
                ].map((r, i) => (
                  <div
                    key={i}
                    style={{
                      display: 'flex', gap: 12, padding: '10px 12px',
                      borderBottom: i < 2 ? '1px solid var(--hairline)' : 'none',
                      alignItems: 'center', cursor: 'pointer',
                    }}
                    onClick={() => navigate(r.to)}
                  >
                    <div style={{ width: 28, height: 28, borderRadius: 7, background: 'var(--surface-3)', color: r.color, display: 'grid', placeItems: 'center', flex: '0 0 auto' }}>
                      {r.icon}
                    </div>
                    <span style={{ fontSize: 'var(--fs-sm)', fontWeight: 500, flex: 1 }}>{r.text}</span>
                    <IcChev size={12} style={{ color: 'var(--muted)' }} />
                  </div>
                ))}
              </div>
            </div>

            {/* Estado del sistema */}
            <div className="card">
              <div className="card-h">
                <h3>Sistema</h3>
                <div className="sub">estado</div>
              </div>
              <div className="card-b">
                <div className="between">
                  <div className="row">
                    <IcBell size={14} style={{ color: 'var(--ok)' }} />
                    <span style={{ fontSize: 'var(--fs-sm)' }}>API operativa</span>
                  </div>
                  <span className="chip chip--ok"><span className="dot" /><IcArrowUp size={10} />online</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  )
}
