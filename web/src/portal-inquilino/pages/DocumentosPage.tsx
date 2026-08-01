import { useState, useRef, useEffect } from 'react'
import {
  IcDownload, IcShield, IcDoc, IcTrend, IcLink, IcClock, IcReceipt,
} from '@/shared/components/ui/Icons'
import { formatDate } from '@/shared/lib/formatters'
import { QueryError } from '@/shared/components/ui/QueryError'
import { useMyDocuments } from '@/features/documents/hooks/useDocuments'
import { useMyContract } from '@/features/me/hooks/useMe'
import { documentService } from '@/features/documents/services/documentService'
import type { DocumentDownloadUrlDto } from '@/features/documents/types/document.types'

function docIcon(mimeType: string) {
  if (mimeType.includes('pdf')) return <IcDoc size={16} />
  if (mimeType.includes('image')) return <IcReceipt size={16} />
  return <IcTrend size={16} />
}

function docColors(mimeType: string): { bg: string; fg: string } {
  if (mimeType.includes('pdf')) return { bg: 'var(--brand-50)', fg: 'var(--brand)' }
  if (mimeType.includes('image')) return { bg: 'var(--ok-50)', fg: 'var(--ok)' }
  return { bg: 'var(--icl-50)', fg: 'var(--icl)' }
}

function fmtBytes(b: number) {
  if (b < 1024) return `${b} B`
  if (b < 1024 * 1024) return `${(b / 1024).toFixed(1)} KB`
  return `${(b / (1024 * 1024)).toFixed(1)} MB`
}

export default function TenantDocumentosPage() {
  const { data: docs, isLoading, isError, refetch } = useMyDocuments()
  const { data: contract } = useMyContract()
  const [urlModal, setUrlModal] = useState<DocumentDownloadUrlDto | null>(null)
  const [countdown, setCountdown] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null)

  function clearTick() {
    if (intervalRef.current) {
      clearInterval(intervalRef.current)
      intervalRef.current = null
    }
  }

  function closeModal() {
    clearTick()
    setUrlModal(null)
    setCountdown(null)
  }

  // Stop the countdown timer if the component unmounts mid-modal.
  useEffect(() => clearTick, [])

  async function handleDownload(docId: string) {
    if (!contract?.id) return
    setError(null)
    clearTick() // never leak a previous timer
    let dto: DocumentDownloadUrlDto
    try {
      dto = await documentService.getDownloadUrl(contract.id, docId)
    } catch {
      setError('No se pudo generar el link de descarga. Intentá de nuevo.')
      return
    }
    setUrlModal(dto)

    const expMs = new Date(dto.expiresAt).getTime()
    intervalRef.current = setInterval(() => {
      const remaining = Math.max(0, Math.round((expMs - Date.now()) / 1000))
      const mm = String(Math.floor(remaining / 60)).padStart(2, '0')
      const ss = String(remaining % 60).padStart(2, '0')
      setCountdown(`${mm}:${ss}`)
      if (remaining === 0) clearTick()
    }, 1000)
  }

  return (
    <div style={{ maxWidth: 420, margin: '0 auto', padding: '20px 18px 80px', display: 'flex', flexDirection: 'column', gap: 14, position: 'relative' }}>
      {/* Header */}
      <div>
        <h2 style={{ fontSize: 20, fontWeight: 600, margin: 0, letterSpacing: '-.01em' }}>Mis documentos</h2>
        <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', marginTop: 2 }}>
          Descargas seguras · links válidos por 5 minutos
        </div>
      </div>

      {/* Lista de documentos */}
      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        {isLoading ? (
          <div style={{ padding: '20px 14px', color: 'var(--muted)', fontSize: 'var(--fs-sm)', textAlign: 'center' }}>Cargando…</div>
        ) : isError ? (
          <QueryError onRetry={() => refetch()} message="No pudimos cargar tus documentos." />
        ) : !docs || docs.length === 0 ? (
          <div style={{ padding: '24px 14px', color: 'var(--muted)', fontSize: 'var(--fs-sm)', textAlign: 'center' }}>
            <IcDoc size={28} style={{ opacity: .3, display: 'block', margin: '0 auto 8px' }} />
            Sin documentos disponibles aún.
          </div>
        ) : docs.map((d, i) => {
          const colors = docColors(d.mimeType)
          return (
            <div
              key={d.id}
              className="between"
              style={{
                padding: '12px 14px',
                borderBottom: i < docs.length - 1 ? '1px solid var(--hairline)' : 'none',
                cursor: 'pointer',
              }}
              onClick={() => handleDownload(d.id)}
            >
              <div className="row" style={{ gap: 10 }}>
                <div style={{ width: 34, height: 34, borderRadius: 8, background: colors.bg, color: colors.fg, display: 'grid', placeItems: 'center', flex: '0 0 auto' }}>
                  {docIcon(d.mimeType)}
                </div>
                <div style={{ minWidth: 0 }}>
                  <div style={{ fontSize: 'var(--fs-sm)', fontWeight: 500 }}>{d.fileName}</div>
                  <div style={{ fontSize: 11, color: 'var(--muted)' }}>{formatDate(d.createdAt.split('T')[0])} · {fmtBytes(d.sizeBytes)}</div>
                </div>
              </div>
              <button
                className="btn btn--ghost btn--sm btn--icon"
                onClick={(e) => { e.stopPropagation(); handleDownload(d.id) }}
              >
                <IcDownload size={14} />
              </button>
            </div>
          )
        })}
      </div>

      {/* Bottom sheet: secure link */}
      {error && (
        <div role="alert" style={{
          background: 'var(--danger-50, #fdecea)', color: 'var(--danger, #b3261e)',
          border: '1px solid var(--danger, #b3261e)', borderRadius: 8, padding: '10px 12px', fontSize: 13,
        }}>
          {error}
        </div>
      )}

      {urlModal && (
        <div
          onClick={closeModal}
          style={{
            position: 'fixed', inset: 0,
            background: 'rgba(20,20,16,.4)',
            display: 'flex', alignItems: 'flex-end',
            zIndex: 200,
          }}
        >
          <div
            onClick={(e) => e.stopPropagation()}
            className="card"
            style={{
              width: '100%', maxWidth: 420, margin: '0 auto',
              borderRadius: '16px 16px 0 0', padding: 18,
              border: 'none', boxShadow: '0 -10px 30px rgba(0,0,0,.18)',
            }}
          >
            <div style={{ width: 36, height: 4, background: 'var(--n-150)', borderRadius: 2, margin: '-2px auto 14px' }} />

            <div className="row" style={{ marginBottom: 10 }}>
              <div style={{ width: 36, height: 36, borderRadius: 9, background: 'var(--brand-50)', color: 'var(--brand)', display: 'grid', placeItems: 'center' }}>
                <IcShield size={18} />
              </div>
              <div>
                <div style={{ fontSize: 'var(--fs-sm)', fontWeight: 600 }}>Link seguro</div>
                <div style={{ fontSize: 11, color: 'var(--muted)' }}>El link expira en 5 minutos</div>
              </div>
            </div>

            <div style={{
              background: 'var(--surface-2)', border: '1px solid var(--hairline)',
              borderRadius: 8, padding: '10px 12px',
              fontFamily: 'var(--font-mono)', fontSize: 11, color: 'var(--ink-soft)',
              wordBreak: 'break-all', lineHeight: 1.5,
            }}>
              {urlModal.url}
            </div>

            {countdown && (
              <div className="between" style={{ marginTop: 8, fontSize: 11 }}>
                <span style={{ color: 'var(--muted)', display: 'inline-flex', alignItems: 'center', gap: 4 }}>
                  <IcClock size={12} /> Expira en
                </span>
                <span className="mono" style={{ color: 'var(--warn)', fontWeight: 600 }}>{countdown}</span>
              </div>
            )}

            <div className="row" style={{ marginTop: 14, gap: 6 }}>
              <button className="btn" style={{ flex: 1, justifyContent: 'center' }} onClick={() => navigator.clipboard.writeText(urlModal.url)}>
                <IcLink size={14} /> Copiar
              </button>
              <button className="btn btn--primary" style={{ flex: 1, justifyContent: 'center' }} onClick={() => window.open(urlModal.url, '_blank')}>
                <IcDownload size={14} /> Descargar
              </button>
            </div>
          </div>
        </div>
      )}

    </div>
  )
}
