import { useState } from 'react'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { IcPlus, IcSearch } from '@/shared/components/ui/Icons'
import { LeadDetailDrawer } from '@/features/leads/components/LeadDetailDrawer'
import { LeadFormModal } from '@/features/leads/components/LeadFormModal'
import { LeadKanbanBoard } from '@/features/leads/components/LeadKanbanBoard'
import type { LeadDto } from '@/features/leads/types/lead.types'

export default function ConsultasPage() {
  const [search, setSearch] = useState('')
  const [selectedLeadId, setSelectedLeadId] = useState<string | null>(null)
  const [showCreate, setShowCreate] = useState(false)

  function handleOpenLead(lead: LeadDto) {
    setSelectedLeadId(lead.id)
  }

  return (
    <>
      <AdminTopbar
        crumbs={['Consultas']}
        right={
          <button className="btn btn--sm btn--primary" onClick={() => setShowCreate(true)}>
            <IcPlus size={12} /> Nueva consulta
          </button>
        }
      />
      <div className="page">
        <div className="page-h">
          <div>
            <h1>Consultas</h1>
            <div className="lead">Seguimiento de leads del sitio y cargas manuales</div>
          </div>
        </div>

        <div className="row" style={{ gap: 8 }}>
          <div className="row" style={{ gap: 8, background: 'var(--surface)', border: '1px solid var(--hairline-2)', borderRadius: 'var(--r-3)', padding: '0 10px', height: 'var(--input-h)', width: 320 }}>
            <IcSearch size={14} style={{ color: 'var(--muted)' }} />
            <input
              style={{ border: 'none', outline: 'none', width: '100%', fontSize: 'var(--fs-sm)', background: 'transparent' }}
              placeholder="Buscar por nombre, email, teléfono o propiedad…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </div>

        <LeadKanbanBoard search={search} onOpenLead={handleOpenLead} />
      </div>

      <LeadDetailDrawer leadId={selectedLeadId} onClose={() => setSelectedLeadId(null)} />
      <LeadFormModal open={showCreate} onClose={() => setShowCreate(false)} />
    </>
  )
}
