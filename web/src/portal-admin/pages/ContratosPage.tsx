import { useState } from 'react'
import { useNavigate } from 'react-router'
import { AdminTopbar } from '../layouts/AdminTopbar'
import {
  IcPlus, IcSearch, IcChevDown, IcDownload, IcChev, IcArrowL, IcArrowR,
} from '@/shared/components/ui/Icons'
import { formatARS, formatDateShort } from '@/shared/lib/formatters'

interface MockContrato {
  id: string
  code: string
  tenant: string
  property: string
  rent: number
  idx: 'ICL' | 'IPC'
  nextAdj: string
  status: 'paid' | 'late' | 'pending' | 'partial'
  overdue: number
}

const MOCK_CONTRATOS: MockContrato[] = [
  { id: 'C-1042', code: 'C-1042', tenant: 'M. Álvarez', property: 'Av. Córdoba 2840 · 7B', rent: 485000, idx: 'ICL', nextAdj: '2026-07-15', status: 'paid', overdue: 0 },
  { id: 'C-1039', code: 'C-1039', tenant: 'F. Pereyra', property: 'J. B. Justo 1190 · PB', rent: 320000, idx: 'IPC', nextAdj: '2026-06-01', status: 'late', overdue: 1 },
  { id: 'C-1037', code: 'C-1037', tenant: 'L. Gutiérrez', property: 'Salguero 720 · 12A', rent: 612000, idx: 'ICL', nextAdj: '2026-07-15', status: 'paid', overdue: 0 },
  { id: 'C-1031', code: 'C-1031', tenant: 'R. Domínguez', property: 'Honduras 5410 · 4C', rent: 410000, idx: 'IPC', nextAdj: '2026-06-01', status: 'pending', overdue: 0 },
  { id: 'C-1028', code: 'C-1028', tenant: 'C. Ibáñez', property: 'Bonpland 2155 · 3B', rent: 540000, idx: 'ICL', nextAdj: '2026-07-15', status: 'paid', overdue: 0 },
  { id: 'C-1024', code: 'C-1024', tenant: 'S. Martínez', property: 'Yerbal 312 · 8D', rent: 295000, idx: 'IPC', nextAdj: '2026-06-01', status: 'late', overdue: 2 },
  { id: 'C-1020', code: 'C-1020', tenant: 'P. Quiroga', property: 'Charcas 4488 · 1A', rent: 720000, idx: 'ICL', nextAdj: '2026-07-15', status: 'partial', overdue: 0 },
  { id: 'C-1018', code: 'C-1018', tenant: 'N. Ferreyra', property: 'Gascón 825 · 6F', rent: 380000, idx: 'IPC', nextAdj: '2026-06-01', status: 'paid', overdue: 0 },
]

const STATUS_MAP = {
  paid: { cls: 'chip--ok', lbl: 'Pagado' },
  late: { cls: 'chip--danger', lbl: 'Vencido' },
  pending: { cls: 'chip--warn', lbl: 'Pendiente' },
  partial: { cls: 'chip--info', lbl: 'Parcial' },
}

const TODAY = new Date('2026-05-07')

export default function ContratosPage() {
  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const [activeTab, setActiveTab] = useState<'all' | 'active' | 'late' | 'ending'>('all')

  const tabs = [
    { k: 'all', lbl: 'Todos · 247' },
    { k: 'active', lbl: 'Vigentes · 224' },
    { k: 'late', lbl: 'Vencidos · 14' },
    { k: 'ending', lbl: 'Finalizando · 9' },
  ] as const

  return (
    <>
      <AdminTopbar
        crumbs={['Contratos']}
        right={
          <button className="btn btn--primary btn--sm">
            <IcPlus size={12} /> Nuevo contrato
          </button>
        }
      />
      <div className="page">
        <div className="page-h">
          <div>
            <h1>Contratos</h1>
            <div className="lead">247 vigentes · 18 finalizan en 90 días</div>
          </div>
          <button className="btn btn--sm">
            <IcDownload size={12} /> Exportar
          </button>
        </div>

        {/* Filtros */}
        <div className="card card-b" style={{ display: 'flex', gap: 10, alignItems: 'center', padding: '12px 14px' }}>
          <div
            className="row"
            style={{ background: 'var(--surface-3)', borderRadius: 6, padding: '0 10px', height: 32, flex: 1 }}
          >
            <IcSearch size={14} />
            <input
              style={{
                border: 'none', outline: 'none', background: 'transparent',
                height: '100%', flex: 1, fontSize: 'var(--fs-sm)', fontFamily: 'inherit',
              }}
              placeholder="Buscar por inquilino, propiedad o código…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <div style={{ width: 1, height: 24, background: 'var(--hairline)' }} />
          <button className="btn btn--sm">Estado <IcChevDown size={12} /></button>
          <button className="btn btn--sm">Índice <IcChevDown size={12} /></button>
          <button className="btn btn--sm">Próximo ajuste <IcChevDown size={12} /></button>
          <button className="btn btn--sm">Propiedad <IcChevDown size={12} /></button>
        </div>

        {/* Tabs */}
        <div className="row" style={{ gap: 4 }}>
          {tabs.map((tab) => (
            <button
              key={tab.k}
              onClick={() => setActiveTab(tab.k)}
              className={activeTab === tab.k ? 'chip chip--solid' : 'chip'}
              style={{ cursor: 'pointer', border: 'none' }}
            >
              {tab.lbl}
            </button>
          ))}
        </div>

        {/* Tabla */}
        <div className="card" style={{ overflow: 'hidden' }}>
          <table className="tbl">
            <thead>
              <tr>
                <th style={{ width: 28 }}><input type="checkbox" /></th>
                <th>Contrato</th>
                <th>Propiedad</th>
                <th>Índice</th>
                <th className="num">Alquiler vigente</th>
                <th>Próximo ajuste</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {MOCK_CONTRATOS.map((c) => {
                const statusInfo = STATUS_MAP[c.status]
                const initials = c.tenant.split(' ').map((s) => s[0]).slice(0, 2).join('')
                const daysUntil = Math.round((new Date(c.nextAdj).getTime() - TODAY.getTime()) / 86400000)
                return (
                  <tr
                    key={c.id}
                    style={{ cursor: 'pointer' }}
                    onClick={() => navigate(`/admin/contratos/${c.id}`)}
                  >
                    <td onClick={(e) => e.stopPropagation()}><input type="checkbox" /></td>
                    <td>
                      <div className="row" style={{ gap: 10 }}>
                        <div className="mono-avatar">{initials}</div>
                        <div>
                          <div style={{ fontWeight: 500 }}>{c.tenant}</div>
                          <div className="mono" style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>{c.code}</div>
                        </div>
                      </div>
                    </td>
                    <td className="muted">{c.property}</td>
                    <td>
                      <span className={`chip chip--${c.idx === 'ICL' ? 'icl' : 'ipc'}`}>
                        <span className="dot" />{c.idx}
                      </span>
                    </td>
                    <td className="num"><b>{formatARS(c.rent)}</b></td>
                    <td>
                      <div>{formatDateShort(c.nextAdj)}</div>
                      <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>en {daysUntil} d</div>
                    </td>
                    <td>
                      <span className={`chip ${statusInfo.cls}`}>
                        <span className="dot" />{statusInfo.lbl}
                      </span>
                      {c.overdue > 0 && (
                        <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--danger)', marginTop: 2 }}>
                          {c.overdue} mes(es)
                        </div>
                      )}
                    </td>
                    <td>
                      <button className="btn btn--ghost btn--sm btn--icon">
                        <IcChev size={12} />
                      </button>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
          <div
            className="between hairline-t"
            style={{ padding: '10px 14px', color: 'var(--muted)', fontSize: 'var(--fs-xs)' }}
          >
            <span>Mostrando 1–8 de 247</span>
            <div className="row">
              <button className="btn btn--ghost btn--sm"><IcArrowL size={12} /></button>
              <span>1 / 31</span>
              <button className="btn btn--ghost btn--sm"><IcArrowR size={12} /></button>
            </div>
          </div>
        </div>
      </div>
    </>
  )
}
