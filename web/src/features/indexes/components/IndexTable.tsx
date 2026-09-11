import { QueryError } from '@/shared/components/ui/QueryError'
import type { IndexValueDto } from '../types/index.types'

interface IndexTableProps {
  rows: IndexValueDto[] | undefined
  isLoading: boolean
  error: unknown
}

const numberFmt = new Intl.NumberFormat('es-AR', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 4,
})

const pctFmt = new Intl.NumberFormat('es-AR', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
  signDisplay: 'exceptZero',
})

function formatPeriod(yyyymmdd: string): string {
  return yyyymmdd.slice(0, 7)
}

function formatPct(v: number | null): string {
  if (v == null) return '—'
  return pctFmt.format(v) + '%'
}

export function IndexTable({ rows, isLoading, error }: IndexTableProps) {
  if (isLoading) {
    return <div className="card" style={{ padding: 24, textAlign: 'center', color: 'var(--muted)' }}>Cargando…</div>
  }
  if (error) {
    const msg = error instanceof Error ? error.message : 'Error desconocido'
    return <QueryError message={`Error: ${msg}`} />
  }
  if (!rows || rows.length === 0) {
    return <div className="card" style={{ padding: 24, textAlign: 'center', color: 'var(--muted)' }}>Sin índices en el rango seleccionado.</div>
  }
  return (
    <div className="card">
      <table className="tbl">
        <thead>
          <tr>
            <th>Período</th>
            <th className="num">Valor</th>
            <th className="num">Variación %</th>
            <th>Fuente</th>
            <th>Sincronizado</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((r) => (
            <tr key={r.id}>
              <td>{formatPeriod(r.period)}</td>
              <td className="num tnum">{numberFmt.format(r.value)}</td>
              <td className="num tnum">{formatPct(r.variationPct)}</td>
              <td className="muted" style={{ fontSize: 'var(--fs-xs)' }}>{r.source}</td>
              <td className="muted" style={{ fontSize: 'var(--fs-xs)' }}>{new Date(r.fetchedAt).toLocaleString('es-AR')}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
