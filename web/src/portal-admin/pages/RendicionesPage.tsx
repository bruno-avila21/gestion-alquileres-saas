import { useState } from 'react'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { QueryError } from '@/shared/components/ui/QueryError'
import { IcCalculator, IcCash, IcDownload, IcReceipt, IcUsers } from '@/shared/components/ui/Icons'
import { formatARSExact } from '@/shared/lib/formatters'
import { downloadBlob } from '@/shared/lib/downloadFile'
import { downloadCsv } from '@/shared/lib/exportCsv'
import { useAllSettlements } from '@/features/owners/hooks/useOwners'
import { ownerService } from '@/features/owners/services/ownerService'
import { SimuladorAjusteModal } from '@/features/owners/components/SimuladorAjusteModal'
import type { OwnerSettlementDto } from '@/features/owners/types/owner.types'

function currentMonthValue(): string {
  return new Date().toISOString().slice(0, 7)
}

/** Primer y último día del mes `"YYYY-MM"`, en ISO `yyyy-MM-dd`. */
function monthRange(month: string): { from: string; to: string } {
  const [y, m] = month.split('-').map(Number)
  const last = new Date(Date.UTC(y, m, 0)).getUTCDate()
  return { from: `${month}-01`, to: `${month}-${String(last).padStart(2, '0')}` }
}

export default function RendicionesPage() {
  const [month, setMonth] = useState(currentMonthValue())
  const [simuladorOpen, setSimuladorOpen] = useState(false)
  const [busy, setBusy] = useState('')
  const [error, setError] = useState('')

  const { from, to } = monthRange(month)
  const { data: settlements, isLoading, isError, refetch } = useAllSettlements(from, to)

  const filas = settlements ?? []
  const bruto = filas.reduce((a, s) => a + s.grossCollected, 0)
  const comision = filas.reduce((a, s) => a + s.commissionAmount, 0)
  const neto = filas.reduce((a, s) => a + s.netToOwner, 0)
  const sinCbu = filas.filter((s) => !s.ownerCbu?.trim())

  /**
   * Lote para el banco: una fila por propietario con su CBU y el neto a transferir.
   * Deja afuera a los que no tienen CBU cargado en vez de exportarlos con la celda
   * vacía — una fila sin CBU en un archivo de transferencias es un error de importación
   * en el homebanking, y ahí ya es tarde para darse cuenta.
   */
  function handleExportarBanco() {
    setError('')
    const conCbu = filas.filter((s) => s.ownerCbu?.trim())
    if (conCbu.length === 0) {
      setError('Ningún propietario del período tiene CBU cargado.')
      return
    }
    downloadCsv(
      `transferencias-${month}.csv`,
      ['Propietario', 'CBU/Alias', 'Importe', 'Moneda', 'Concepto'],
      conCbu.map((s) => [
        s.ownerName,
        s.ownerCbu ?? '',
        s.netToOwner.toFixed(2),
        'ARS',
        `Liquidacion ${month}`,
      ]),
    )
  }

  /**
   * Liquidación masiva: genera el comprobante de cada propietario del período, uno por uno.
   * Se descargan de a uno y no como un lote cerrado porque hoy las liquidaciones se
   * calculan al vuelo y no se persisten: no hay entidad de lote que cerrar ni estado
   * "transferido" que marcar. Eso es el bloque siguiente, no algo para simular acá.
   */
  async function handleLiquidacionMasiva() {
    setError('')
    if (filas.length === 0) return
    setBusy('masiva')
    try {
      for (const s of filas) {
        const { blob, fileName } = await ownerService.downloadSettlementPdf(s.ownerId, from, to)
        downloadBlob(blob, fileName)
      }
    } catch {
      setError('No pudimos generar todos los comprobantes. Probá de nuevo.')
    } finally {
      setBusy('')
    }
  }

  async function handleRecibo(s: OwnerSettlementDto) {
    setError('')
    setBusy(s.ownerId)
    try {
      const { blob, fileName } = await ownerService.downloadSettlementPdf(s.ownerId, from, to)
      downloadBlob(blob, fileName)
    } catch {
      setError('No pudimos generar el comprobante.')
    } finally {
      setBusy('')
    }
  }

  const kpis = [
    { lbl: 'Cobrado a inquilinos', val: formatARSExact(bruto), hint: `${filas.length} propietario${filas.length === 1 ? '' : 's'} con cobranzas`, icon: <IcCash size={18} />, color: 'var(--brand)' },
    { lbl: 'A liquidar', val: formatARSExact(neto), hint: 'Neto a transferir en el período', icon: <IcReceipt size={18} />, color: 'var(--ok)' },
    { lbl: 'Comisión', val: formatARSExact(comision), hint: bruto > 0 ? `${((comision / bruto) * 100).toFixed(1)}% del cobrado` : 'Sin cobranzas', icon: <IcCash size={18} />, color: 'var(--warn)' },
    { lbl: 'Sin CBU cargado', val: String(sinCbu.length), hint: sinCbu.length === 0 ? 'Todos listos para transferir' : 'No entran en el archivo del banco', icon: <IcUsers size={18} />, color: sinCbu.length === 0 ? 'var(--ok)' : 'var(--danger)' },
  ]

  return (
    <>
      <AdminTopbar
        crumbs={['Liquidaciones']}
        right={
          <button
            className="btn btn--sm btn--primary"
            disabled={filas.length === 0 || busy === 'masiva'}
            onClick={handleLiquidacionMasiva}
          >
            <IcReceipt size={12} /> {busy === 'masiva' ? 'Generando…' : 'Nueva liquidación masiva'}
          </button>
        }
      />
      <div className="page">
        <div className="page-h">
          <div>
            <h1>Liquidaciones a propietarios</h1>
            <div className="lead">Cobranzas del período, menos comisión, netas por propietario</div>
          </div>
          <div className="row" style={{ gap: 8 }}>
            <button className="btn btn--sm" onClick={() => setSimuladorOpen(true)}>
              <IcCalculator size={12} /> Simular ajuste ICL/IPC
            </button>
            <button className="btn btn--sm" disabled={filas.length === 0} onClick={handleExportarBanco}>
              <IcDownload size={12} /> Exportar a banco (CBU/Alias)
            </button>
          </div>
        </div>

        <div className="row" style={{ gap: 12, alignItems: 'flex-end' }}>
          <div>
            <label className="label" htmlFor="periodo">Período</label>
            <input
              id="periodo" className="input select--inline" type="month" style={{ marginTop: 4 }}
              value={month} onChange={(e) => setMonth(e.target.value)}
            />
          </div>
          <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', paddingBottom: 10 }}>
            Base de cálculo: cobranzas efectivamente acreditadas entre el {from} y el {to}.
          </div>
        </div>

        <div className="grid-4">
          {kpis.map((k) => (
            <div key={k.lbl} className="card stat">
              <div className="between">
                <span className="lbl">{k.lbl}</span>
                <span style={{ color: k.color, opacity: 0.8 }}>{k.icon}</span>
              </div>
              <div className="val">{isLoading ? '…' : k.val}</div>
              <div style={{ marginTop: 8, fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>{k.hint}</div>
            </div>
          ))}
        </div>

        {error ? <div role="alert" style={{ fontSize: 'var(--fs-sm)', color: 'var(--danger)' }}>{error}</div> : null}

        {isError ? (
          <QueryError onRetry={() => refetch()} />
        ) : (
          <div className="card">
            <div className="card-h">
              <div>
                <h3>Detalle por propietario</h3>
                <div className="sub">{filas.length} con cobranzas en el período</div>
              </div>
            </div>
            {isLoading ? (
              <div style={{ padding: 32, textAlign: 'center', color: 'var(--muted)' }}>Cargando…</div>
            ) : filas.length === 0 ? (
              <div style={{ padding: 40, textAlign: 'center', color: 'var(--muted)' }}>
                No hubo cobranzas acreditadas en este período.
              </div>
            ) : (
              <table className="tbl">
                <thead>
                  <tr>
                    <th>Propietario</th>
                    <th>CBU / Alias</th>
                    <th className="num">Cobrado</th>
                    <th className="num">Comisión</th>
                    <th className="num">Neto a transferir</th>
                    <th>Estado</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {filas.map((s) => {
                    const listo = !!s.ownerCbu?.trim()
                    return (
                      <tr key={s.ownerId}>
                        <td>
                          <div style={{ fontWeight: 500 }}>{s.ownerName}</div>
                          <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', marginTop: 2 }}>
                            {s.lines.length} propiedad{s.lines.length === 1 ? '' : 'es'}
                          </div>
                        </td>
                        <td className="mono muted" style={{ fontSize: 'var(--fs-xs)' }}>
                          {s.ownerCbu?.trim() || '—'}
                        </td>
                        <td className="num">{formatARSExact(s.grossCollected)}</td>
                        <td className="num" style={{ color: 'var(--danger)' }}>
                          −{formatARSExact(s.commissionAmount)}
                        </td>
                        <td className="num"><b>{formatARSExact(s.netToOwner)}</b></td>
                        <td>
                          <span className={`chip ${listo ? 'chip--ok' : 'chip--warn'}`}>
                            <span className="dot" />
                            {listo ? 'Listo para transferir' : 'Falta CBU'}
                          </span>
                        </td>
                        <td style={{ textAlign: 'right' }}>
                          <button
                            className="btn btn--sm"
                            disabled={busy === s.ownerId}
                            onClick={() => handleRecibo(s)}
                          >
                            <IcDownload size={12} /> {busy === s.ownerId ? 'Generando…' : 'Comprobante'}
                          </button>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            )}
          </div>
        )}
      </div>

      <SimuladorAjusteModal open={simuladorOpen} onClose={() => setSimuladorOpen(false)} />
    </>
  )
}
