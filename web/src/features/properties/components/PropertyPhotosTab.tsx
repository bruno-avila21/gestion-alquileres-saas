import { useRef, useState } from 'react'
import {
  usePropertyPhotos, useUploadPropertyPhoto, useDeletePropertyPhoto, useSetCoverPhoto,
} from '../hooks/useProperties'
import type { PropertyPhotoDto } from '../types/property.types'
import { resolvePropertyPhotoUrl } from '../utils/resolvePhotoUrl'
import { IcCamera, IcStar, IcUpload } from '@/shared/components/ui/Icons'
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog'
import { QueryError } from '@/shared/components/ui/QueryError'

const ACCEPTED_TYPES = ['image/jpeg', 'image/png', 'image/webp']
const MAX_SIZE_BYTES = 10 * 1024 * 1024

export function PropertyPhotosTab({ propertyId }: { propertyId: string }) {
  const { data: photos, isLoading, error, refetch } = usePropertyPhotos(propertyId)
  const upload = useUploadPropertyPhoto(propertyId)
  const setCover = useSetCoverPhoto(propertyId)
  const remove = useDeletePropertyPhoto(propertyId)

  const fileRef = useRef<HTMLInputElement>(null)
  const [confirmDelete, setConfirmDelete] = useState<PropertyPhotoDto | null>(null)
  const [uploadError, setUploadError] = useState('')
  const [progress, setProgress] = useState<{ done: number; total: number } | null>(null)

  async function handleFiles(e: React.ChangeEvent<HTMLInputElement>) {
    const files = Array.from(e.target.files ?? [])
    if (files.length === 0) return
    setUploadError('')
    setProgress({ done: 0, total: files.length })

    const failed: string[] = []
    for (const file of files) {
      if (!ACCEPTED_TYPES.includes(file.type) || file.size > MAX_SIZE_BYTES) {
        failed.push(file.name)
        setProgress((p) => (p ? { ...p, done: p.done + 1 } : p))
        continue
      }
      try {
        // Secuencial: la API valida y guarda una foto por request, en orden es más predecible
        // que el usuario vea aparecer las miniaturas en el mismo orden en que las eligió.
        await upload.mutateAsync(file)
      } catch {
        failed.push(file.name)
      }
      setProgress((p) => (p ? { ...p, done: p.done + 1 } : p))
    }

    setProgress(null)
    if (failed.length > 0) {
      setUploadError(`No se pudo subir: ${failed.join(', ')}. Verificá que sean JPG/PNG/WEBP de hasta 10 MB.`)
    }
    if (fileRef.current) fileRef.current.value = ''
  }

  return (
    <>
      <div className="between" style={{ marginBottom: 12 }}>
        <div style={{ fontSize: 'var(--fs-sm)', fontWeight: 500 }}>
          {photos?.length ?? 0} foto{(photos?.length ?? 0) !== 1 ? 's' : ''}
        </div>
        <div className="row">
          <input
            id={`property-photo-input-${propertyId}`}
            type="file"
            accept={ACCEPTED_TYPES.join(',')}
            multiple
            ref={fileRef}
            style={{ display: 'none' }}
            onChange={handleFiles}
          />
          <button
            className="btn btn--sm btn--primary"
            onClick={() => fileRef.current?.click()}
            disabled={!!progress}
          >
            <IcUpload size={12} /> {progress ? `Subiendo ${progress.done}/${progress.total}…` : 'Subir fotos'}
          </button>
        </div>
      </div>

      {uploadError && (
        <div role="alert" style={{ fontSize: 'var(--fs-xs)', color: 'var(--danger)', marginBottom: 10 }}>
          {uploadError}
        </div>
      )}
      {error && <QueryError message="No pudimos cargar las fotos." onRetry={() => refetch()} />}

      {isLoading ? (
        <div style={{ padding: 24, textAlign: 'center', color: 'var(--muted)' }}>Cargando…</div>
      ) : !photos || photos.length === 0 ? (
        <div style={{ padding: 32, textAlign: 'center', color: 'var(--muted)' }}>
          <IcCamera size={28} style={{ opacity: .3, display: 'block', margin: '0 auto 8px' }} />
          Sin fotos cargadas.
        </div>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(140px, 1fr))', gap: 10 }}>
          {photos.map((photo) => (
            <div key={photo.id} className="card" style={{ padding: 0, overflow: 'hidden', position: 'relative' }}>
              <img
                src={resolvePropertyPhotoUrl(photo.url)}
                alt=""
                style={{ width: '100%', height: 100, objectFit: 'cover', display: 'block', background: 'var(--surface-2)' }}
              />
              {photo.isCover && (
                <span className="chip chip--solid" style={{ position: 'absolute', top: 6, left: 6, height: 18, fontSize: 9, padding: '0 6px' }}>
                  Portada
                </span>
              )}
              <div className="row" style={{ padding: 6, gap: 4, justifyContent: 'flex-end', borderTop: '1px solid var(--hairline)' }}>
                {!photo.isCover && (
                  <button
                    className="btn btn--ghost btn--sm btn--icon"
                    title="Marcar como portada"
                    aria-label="Marcar como portada"
                    onClick={() => setCover.mutate(photo.id)}
                    disabled={setCover.isPending}
                  >
                    <IcStar size={13} />
                  </button>
                )}
                <button
                  className="btn btn--ghost btn--sm btn--icon"
                  style={{ color: 'var(--danger)' }}
                  title="Eliminar foto"
                  aria-label="Eliminar foto"
                  onClick={() => setConfirmDelete(photo)}
                >
                  ×
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      <ConfirmDialog
        open={!!confirmDelete}
        title="Eliminar foto"
        description="La foto se eliminará de forma permanente."
        confirmLabel="Eliminar"
        destructive
        onConfirm={() => { if (confirmDelete) remove.mutate(confirmDelete.id); setConfirmDelete(null) }}
        onCancel={() => setConfirmDelete(null)}
      />
    </>
  )
}
