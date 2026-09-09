import { useState } from 'react'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { IcPlus } from '@/shared/components/ui/Icons'
import { SearchInput } from '@/shared/components/ui/SearchInput'
import { LeadDetailDrawer } from '@/features/leads/components/LeadDetailDrawer'
import { LeadFormModal } from '@/features/leads/components/LeadFormModal'
import { LeadKanbanBoard } from '@/features/leads/components/LeadKanbanBoard'
import { LeadListTable } from '@/features/leads/components/LeadListTable'
import { LeadMetrics } from '@/features/leads/components/LeadMetrics'
import { useLeads } from '@/features/leads/hooks/useLeads'
import type { LeadDto, LeadSource } from '@/features/leads/types/lead.types'

type Vista = 'tablero' | 'lista'

/**
 * Los orígenes son los dos que el producto realmente distingue hoy: una consulta que
 * entró por el sitio público y una cargada a mano por el operador. No hay chips de
 * Zonaprop ni Argenprop como en la maqueta: no existe esa integración, y un filtro que
 * siempre devuelve cero es peor que no tenerlo.
 */
const ORIGENES: { value: '' | LeadSource; label: string }[] = [
  { value: '', label: 'Todos los orígenes' },
  { value: 'Website', label: 'Sitio web' },
  { value: 'Manual', label: 'Carga manual' },
]

const BOARD_PAGE_SIZE = 200

export default function ConsultasPage() {
  const [search, setSearch] = useState('')
  const [source, setSource] = useState<'' | LeadSource>('')
  const [vista, setVista] = useState<Vista>('tablero')
  const [selectedLeadId, setSelectedLeadId] = useState<string | null>(null)
  const [showCreate, setShowCreate] = useState(false)

  // La lista comparte la misma consulta que el tablero (mismos filtros, mismo pageSize),
  // así que cambiar de vista no dispara una llamada nueva: TanStack Query la sirve de caché.
  const { data, isLoading } = useLeads({ search, page: 1, pageSize: BOARD_PAGE_SIZE })
  const visibles = (data?.items ?? []).filter((l) => !source || l.source === source)

  function handleOpenLead(lead: LeadDto) {
    setSelectedLeadId(lead.id)
  }

  return (
    <>
      <AdminTopbar
        crumbs={['CRM & Leads']}
        right={
          <button className="btn btn--sm btn--primary" onClick={() => setShowCreate(true)}>
            <IcPlus size={12} /> Nueva consulta
          </button>
        }
      />
      <div className="page">
        <div className="page-h">
          <div>
            <h1>CRM &amp; Leads</h1>
            <div className="lead">Pipeline comercial: consultas del sitio y cargas manuales</div>
          </div>
        </div>

        <LeadMetrics />

        <div className="row" style={{ gap: 10, flexWrap: 'wrap' }}>
          <SearchInput
            width={340}
            value={search}
            onChange={setSearch}
            placeholder="Buscar por nombre, email, teléfono o propiedad…"
            ariaLabel="Buscar consultas"
          />

          <select
            className="select select--inline"
            aria-label="Filtrar por origen"
            value={source}
            onChange={(e) => setSource(e.target.value as '' | LeadSource)}
          >
            {ORIGENES.map((o) => (
              <option key={o.value || 'all'} value={o.value}>{o.label}</option>
            ))}
          </select>

          <div className="seg" role="group" aria-label="Vista" style={{ marginLeft: 'auto' }}>
            <button
              type="button"
              className={vista === 'tablero' ? 'on' : undefined}
              aria-pressed={vista === 'tablero'}
              onClick={() => setVista('tablero')}
            >
              Tablero
            </button>
            <button
              type="button"
              className={vista === 'lista' ? 'on' : undefined}
              aria-pressed={vista === 'lista'}
              onClick={() => setVista('lista')}
            >
              Lista
            </button>
          </div>
        </div>

        {vista === 'tablero' ? (
          <LeadKanbanBoard search={search} source={source} onOpenLead={handleOpenLead} />
        ) : (
          <LeadListTable leads={visibles} isLoading={isLoading} onOpen={handleOpenLead} />
        )}
      </div>

      <LeadDetailDrawer leadId={selectedLeadId} onClose={() => setSelectedLeadId(null)} />
      <LeadFormModal open={showCreate} onClose={() => setShowCreate(false)} />
    </>
  )
}
