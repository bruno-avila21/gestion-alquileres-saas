import { useState } from 'react'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { QueryError } from '@/shared/components/ui/QueryError'
import { IcCash, IcDownload } from '@/shared/components/ui/Icons'
import { formatARS, formatPct } from '@/shared/lib/formatters'
import { downloadBlob } from '@/shared/lib/downloadFile'
import { useOwners, useOwnerSettlement } from '@/features/owners/hooks/useOwners'
import { ownerService } from '@/features/owners/services/ownerService'

function currentMonthValue(): string {
  return new Date().toISOString().slice(0, 7)
}

/** `"YYYY-MM"` de un <input type="month"> al primer día del mes, en ISO `yyyy-MM-dd`. */
function monthToIsoDate(month: string): string {
  return `${month}-01`
}

interface SubmittedQuery {
  ownerId: string
  from: string
  to: string
}

export default function RendicionesPage() {
  const { data: owners, isLoading: ownersLoading, isError: ownersError, refetch: refetchOwners } = useOwners()

  const [ownerId, setOwnerId] = useState('')
  const [fromMonth, setFromMonth] = useState(currentMonthValue())
  const [toMonth, setToMonth] = useState(currentMonthValue())
  const [submitted, setSubmitted] = useState<SubmittedQuery | null>(null)
  const [downloading, setDownloading] = useState(false)
  const [downloadError, setDownloadError] = useState('')

  const from = submitted?.from ?? monthToIsoDate(fromMonth)
  const to = submitted?.to ?? monthToIsoDate(toMonth)
  const {
    data: settlement, isLoading, isError, refetch,
  } = useOwnerSettlement(submitted?.ownerId ?? null, from, to, !!submitted)

  const canQuery = !!ownerId && !!fromMonth && !!toMonth

  function handleVer() {
    if (!canQuery) return
    setDownloadError('')
    setSubmitted({ ownerId, from: monthToIsoDate(fromMonth), to: monthToIsoDate(toMonth) })
  }

  async function handleDownloadPdf() {
    if (!submitted) return
    setDownloadError('')
    setDownloading(true)
    try {
      const { blob, fileName } = await ownerService.downloadSettlementPdf(submitted.ownerId, submitted.from, submitted.to)
      downloadBlob(blob, fileName)
    } catch {
      setDownloadError('No pudimos generar el PDF. Probá de nuevo.')
    } finally {
      setDownloading(false)
    }
  }

  return (
    <>
      <AdminTopbar crumbs={['Rendiciones']} />
      <div className="page">
        <div className="page-h">
          <div>
            <h1>Rendiciones</h1>
            <div className="lead">Liquidación de cobranzas a propietarios, por período</div>
          </div>
        </div>

        <div className="row" style={{ gap: 16, alignItems: 'flex-end', flexWrap: 'wrap' }}>
          <div>
            <label className="label" htmlFor="owner">Propietario</label>
            <select
              id="owner"
              className="select"
              style={{ marginTop: 4, minWidth: 220 }}
              value={ownerId}
              onChange={(e) => setOwnerId(e.target.value)}
              disabled={ownersLoading}
            >
              <option value="">Seleccioná un propietario…</option>
              {(owners ?? []).map((o) => (
                <option key={o.id} value={o.id}>{o.name}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="label" htmlFor="from">Desde</label>
            <input
              id="from" className="input" type="month" style={{ marginTop: 4 }}
              value={fromMonth} onChange={(e) => setFromMonth(e.target.value)}
            />
          </div>
          <div>
            <label className="label" htmlFor="to">Hasta</label>
            <input
              id="to" className="input" type="month" style={{ marginTop: 4 }}
              value={toMonth} onChange={(e) => setToMonth(e.target.value)}
            />
          </div>
          <button className="btn btn--primary" onClick={handleVer} disabled={!canQuery}>
            Ver
          </button>
        </div>

        {ownersError && (
          <QueryError onRetry={() => refetchOwners()} message="No pudimos cargar los propietarios." />
        )}

        {downloadError && (
          <div role="alert" style={{ fontSize: 'var(--fs-xs)', color: 'var(--danger)' }}>{downloadError}</div>
        )}

        {!submitted ? (
          <div className="card" style={{ padding: 48, textAlign: 'center', color: 'var(--muted)' }}>
            Elegí un propietario y un período, y tocá "Ver".
          </div>
        ) : isLoading ? (
          <div className="card" style={{ padding: 48, textAlign: 'center', color: 'var(--muted)' }}>Cargando…</div>
        ) : isError ? (
          <QueryError onRetry={() => refetch()} message="No pudimos calcular la rendición." />
        ) : !settlement || settlement.lines.length === 0 ? (
          <div className="card" style={{ padding: 48, textAlign: 'center', color: 'var(--muted)' }}>
            <IcCash size={32} style={{ margin: '0 auto 8px', display: 'block' }} />
            Sin cobranzas registradas en el período.
          </div>
        ) : (
          <div className="card">
            <div className="card-h">
              <h3>{settlement.ownerName}</h3>
              <button className="btn btn--sm" onClick={handleDownloadPdf} disabled={downloading}>
                <IcDownload size={12} /> {downloading ? 'Generando…' : 'Descargar PDF'}
              </button>
            </div>
            <table className="tbl">
              <thead>
                <tr>
                  <th>Inmueble</th>
                  <th className="num">Cobrado</th>
                  <th className="num">Comisión %</th>
                  <th className="num">Comisión</th>
                  <th className="num">Neto</th>
                </tr>
              </thead>
              <tbody>
                {settlement.lines.map((line) => (
                  <tr key={line.contractId}>
                    <td>{line.propertyAddress}</td>
                    <td className="num">{formatARS(line.collected)}</td>
                    <td className="num">{formatPct(line.commissionPct)}</td>
                    <td className="num">{formatARS(line.commission)}</td>
                    <td className="num"><b>{formatARS(line.net)}</b></td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <td style={{ fontWeight: 600, paddingLeft: 12 }}>Total cobrado</td>
                  <td className="num" style={{ fontWeight: 700 }}>{formatARS(settlement.grossCollected)}</td>
                  <td colSpan={3} />
                </tr>
                <tr>
                  <td style={{ fontWeight: 600, paddingLeft: 12 }}>Comisión de administración</td>
                  <td className="num" style={{ fontWeight: 700 }}>-{formatARS(settlement.commissionAmount)}</td>
                  <td colSpan={3} />
                </tr>
                <tr>
                  <td style={{ fontWeight: 700, paddingLeft: 12 }}>NETO A LIQUIDAR</td>
                  <td className="num" style={{ fontWeight: 700, color: 'var(--ok)' }}>{formatARS(settlement.netToOwner)}</td>
                  <td colSpan={3} />
                </tr>
              </tfoot>
            </table>
          </div>
        )}
      </div>
    </>
  )
}
