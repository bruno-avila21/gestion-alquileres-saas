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
  if (v == null) return '\u2014'
  return pctFmt.format(v) + '%'
}

export function IndexTable({ rows, isLoading, error }: IndexTableProps) {
  if (isLoading) return <div className="p-4 text-slate-500">Cargando\u2026</div>
  if (error) {
    const msg = error instanceof Error ? error.message : 'Error desconocido'
    return (
      <div role="alert" className="p-4 text-red-700 bg-red-50 rounded">
        Error: {msg}
      </div>
    )
  }
  if (!rows || rows.length === 0) {
    return (
      <div className="p-4 text-slate-500">Sin índices en el rango seleccionado.</div>
    )
  }
  return (
    <table className="w-full text-sm border-collapse">
      <thead>
        <tr className="bg-slate-100">
          <th className="text-left p-2">Período</th>
          <th className="text-right p-2">Valor</th>
          <th className="text-right p-2">Variación %</th>
          <th className="text-left p-2">Fuente</th>
          <th className="text-left p-2">Sincronizado</th>
        </tr>
      </thead>
      <tbody>
        {rows.map((r) => (
          <tr key={r.id} className="border-b border-slate-200">
            <td className="p-2">{formatPeriod(r.period)}</td>
            <td className="p-2 text-right tabular-nums">{numberFmt.format(r.value)}</td>
            <td className="p-2 text-right tabular-nums">{formatPct(r.variationPct)}</td>
            <td className="p-2">{r.source}</td>
            <td className="p-2">{new Date(r.fetchedAt).toLocaleString('es-AR')}</td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
