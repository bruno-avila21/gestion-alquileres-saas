import { useState } from 'react'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { IcDoc, IcDownload, IcShield } from '@/shared/components/ui/Icons'
import { QueryError } from '@/shared/components/ui/QueryError'
import { formatDate } from '@/shared/lib/formatters'
import { useAllDocuments, useDocumentDownloadUrl, useSetDocumentVisibility } from '@/features/documents/hooks/useDocuments'
import type { DocumentDto } from '@/features/documents/types/document.types'
import { PaginationBar } from '@/shared/components/ui/PaginationBar'
import { useDebounce } from '@/shared/hooks/useDebounce'

const PAGE_SIZE = 20

function formatBytes(bytes: number) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function DownloadButton({ doc }: { doc: DocumentDto }) {
  const getUrl = useDocumentDownloadUrl(doc.contractId, doc.id)

  async function handleDownload() {
    const result = await getUrl.mutateAsync()
    window.open(result.url, '_blank')
  }

  return (
    <button
      className="btn btn--ghost btn--icon btn--sm"
      onClick={handleDownload}
      disabled={getUrl.isPending}
      title="Descargar"
      aria-label={`Descargar ${doc.fileName}`}
    >
      <IcDownload size={14} />
    </button>
  )
}

// Share/unshare a document with the tenant (audit A2). Documents are private by default; the tenant
// portal only shows those toggled visible here.
function VisibilityToggle({ doc }: { doc: DocumentDto }) {
  const setVisibility = useSetDocumentVisibility()
  const visible = doc.isVisibleToTenant
  return (
    <button
      className={`btn btn--sm${visible ? ' btn--primary' : ''}`}
      disabled={setVisibility.isPending}
      onClick={() => setVisibility.mutate({ contractId: doc.contractId, docId: doc.id, isVisibleToTenant: !visible })}
      aria-pressed={visible}
      title={visible ? 'Visible para el inquilino — click para ocultar' : 'Privado — click para compartir con el inquilino'}
    >
      <IcShield size={13} /> {visible ? 'Visible' : 'Privado'}
    </button>
  )
}

export default function DocumentosAdminPage() {
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(0)
  const debouncedSearch = useDebounce(search)

  // Server-side pagination (audit M10): the API returns one page + the total, so the whole dataset is
  // pageable without loading it all. Search runs server-side too, so it spans every page, not just the
  // rows currently loaded.
  const { data, isLoading, isError, refetch } = useAllDocuments({
    page: page + 1, pageSize: PAGE_SIZE, search: debouncedSearch || undefined,
  })
  const documents = data?.items ?? []
  const total = data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))

  return (
    <>
      <AdminTopbar crumbs={['Documentos']} />
      <div className="page">
        <div className="page-h">
          <div>
            <h1>Documentos</h1>
            <div className="lead">Archivos adjuntos de todos los contratos</div>
          </div>
        </div>

        <div className="row" style={{ gap: 8 }}>
          <input
            className="input input--sm"
            style={{ width: 280 }}
            placeholder="Buscar por nombre de archivo…"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(0) }}
          />
        </div>

        {isLoading ? (
          <div className="card" style={{ padding: 48, textAlign: 'center', color: 'var(--muted)' }}>Cargando…</div>
        ) : isError ? (
          <QueryError onRetry={() => refetch()} message="No pudimos cargar los documentos." />
        ) : documents.length === 0 ? (
          <div className="card" style={{ padding: 48, textAlign: 'center', color: 'var(--muted)' }}>
            <IcDoc size={32} style={{ margin: '0 auto 8px', display: 'block' }} />
            Sin documentos
          </div>
        ) : (
          <div className="card">
            <table className="tbl">
              <thead>
                <tr>
                  <th>Archivo</th>
                  <th>Tipo</th>
                  <th className="num">Tamaño</th>
                  <th>Contrato</th>
                  <th>Subido</th>
                  <th>Inquilino</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {documents.map((d) => (
                  <tr key={d.id}>
                    <td style={{ fontWeight: 500 }}>{d.fileName}</td>
                    <td className="muted" style={{ fontSize: 'var(--fs-xs)' }}>{d.mimeType}</td>
                    <td className="num muted" style={{ fontSize: 'var(--fs-xs)' }}>{formatBytes(d.sizeBytes)}</td>
                    <td className="muted" style={{ fontSize: 'var(--fs-xs)', fontFamily: 'monospace' }}>
                      {d.contractId.slice(0, 8)}…
                    </td>
                    <td className="muted" style={{ fontSize: 'var(--fs-xs)' }}>
                      {formatDate(d.createdAt.split('T')[0])}
                    </td>
                    <td>
                      <VisibilityToggle doc={d} />
                    </td>
                    <td>
                      <DownloadButton doc={d} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <PaginationBar page={page} totalPages={totalPages} total={total} pageSize={PAGE_SIZE} onPageChange={setPage} />
          </div>
        )}
      </div>
    </>
  )
}
