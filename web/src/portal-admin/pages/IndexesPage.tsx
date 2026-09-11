import { useState } from 'react'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { useIndexes } from '@/features/indexes/hooks/useIndexes'
import { IndexTable } from '@/features/indexes/components/IndexTable'
import { SyncIndexDialog } from '@/features/indexes/components/SyncIndexDialog'
import type { IndexType } from '@/features/indexes/types/index.types'

function firstOfMonthISO(monthsAgo = 0): string {
  const d = new Date()
  d.setUTCDate(1)
  d.setUTCMonth(d.getUTCMonth() - monthsAgo)
  return d.toISOString().slice(0, 10)
}

export default function IndexesPage() {
  const [type, setType] = useState<IndexType>('ICL')
  const [from, setFrom] = useState(firstOfMonthISO(12))
  const [to, setTo] = useState(firstOfMonthISO(0))
  const [dialogOpen, setDialogOpen] = useState(false)

  const { data, isLoading, error } = useIndexes(type, from, to)

  return (
    <>
      <AdminTopbar
        crumbs={['Índices']}
        right={<button className="btn btn--sm btn--primary" onClick={() => setDialogOpen(true)}>Sincronizar</button>}
      />
      <div className="page">
        <div className="page-h">
          <div>
            <h1>Índices BCRA/INDEC</h1>
            <div className="lead">Valores sincronizados de ICL e IPC</div>
          </div>
        </div>

        <div className="row" style={{ gap: 16, alignItems: 'flex-end', flexWrap: 'wrap' }}>
          <div>
            <label className="label">Tipo</label>
            <div className="row" style={{ gap: 6, marginTop: 4 }}>
              {(['ICL', 'IPC'] as const).map((t) => (
                <button
                  key={t}
                  className={`btn btn--sm${type === t ? ' btn--primary' : ''}`}
                  onClick={() => setType(t)}
                  aria-pressed={type === t}
                >
                  {t}
                </button>
              ))}
            </div>
          </div>
          <div>
            <label className="label" htmlFor="from">Desde</label>
            <input id="from" className="input input--sm" type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
          </div>
          <div>
            <label className="label" htmlFor="to">Hasta</label>
            <input id="to" className="input input--sm" type="date" value={to} onChange={(e) => setTo(e.target.value)} />
          </div>
        </div>

        <IndexTable rows={data} isLoading={isLoading} error={error} />

        <SyncIndexDialog
          open={dialogOpen}
          onOpenChange={setDialogOpen}
          defaultIndexType={type}
        />
      </div>
    </>
  )
}
