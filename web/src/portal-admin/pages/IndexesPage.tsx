import { useState } from 'react'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { useIndexes } from '@/features/indexes/hooks/useIndexes'
import { IndexTable } from '@/features/indexes/components/IndexTable'
import { SyncIndexDialog } from '@/features/indexes/components/SyncIndexDialog'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
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
        right={<Button size="sm" onClick={() => setDialogOpen(true)}>Sincronizar</Button>}
      />
    <div className="p-6 space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Índices BCRA/INDEC</h1>
      </div>

      <div className="flex gap-4 items-end flex-wrap">
        <div>
          <Label>Tipo</Label>
          <div className="flex gap-2 mt-1">
            {(['ICL', 'IPC'] as const).map((t) => (
              <Button
                key={t}
                variant={type === t ? 'default' : 'outline'}
                onClick={() => setType(t)}
                aria-pressed={type === t}
              >
                {t}
              </Button>
            ))}
          </div>
        </div>
        <div>
          <Label htmlFor="from">Desde</Label>
          <Input
            id="from"
            type="date"
            value={from}
            onChange={(e) => setFrom(e.target.value)}
          />
        </div>
        <div>
          <Label htmlFor="to">Hasta</Label>
          <Input
            id="to"
            type="date"
            value={to}
            onChange={(e) => setTo(e.target.value)}
          />
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
