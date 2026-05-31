import { useRef, useState } from 'react'
import { useParams } from 'react-router'
import { AdminTopbar } from '../layouts/AdminTopbar'
import {
  IcEdit, IcEllipsis, IcPin, IcArrowUp,
  IcShield, IcBell, IcDownload, IcUpload, IcPlus,
  IcCalendar, IcDoc, IcChev, IcLink,
} from '@/shared/components/ui/Icons'
import { formatARS, formatDate } from '@/shared/lib/formatters'
import {
  useContractById,
  useRentHistory,
  useTransactions,
  useRegisterPayment,
  useApplyAdjustment,
  useAdjustmentProjection,
} from '@/features/contracts/hooks/useContracts'
import { useContractDocuments, useUploadDocument, useDeleteDocument } from '@/features/documents/hooks/useDocuments'
import { documentService } from '@/features/documents/services/documentService'
import type { ContractDto } from '@/features/contracts/types/contract.types'
import type { DocumentDownloadUrlDto } from '@/features/documents/types/document.types'

type TabKey = 'overview' | 'payments' | 'adjustments' | 'documents'

const TABS = [
  { k: 'overview' as TabKey, lbl: 'Resumen' },
  { k: 'payments' as TabKey, lbl: 'Historial de pagos' },
  { k: 'adjustments' as TabKey, lbl: 'Historial de ajustes' },
  { k: 'documents' as TabKey, lbl: 'Documentos' },
]

function CalcCell({
  k, v, sub, accent, solid,
}: {
  k: string; v: string; sub: React.ReactNode; accent?: string; solid?: boolean
}) {
  return (
    <div
      style={{
        padding: '16px 18px',
        borderRadius: 'var(--r-4)',
        background: solid ? 'var(--brand)' : 'var(--surface-2)',
        color: solid ? 'var(--on-brand)' : 'var(--ink)',
        border: solid ? 'none' : '1px solid var(--hairline)',
        textAlign: 'center',
      }}
    >
      <div style={{ fontSize: 'var(--fs-xs)', textTransform: 'uppercase', letterSpacing: '.05em', color: solid ? 'rgba(255,255,255,.75)' : 'var(--muted)', fontWeight: 500 }}>{k}</div>
      <div style={{ fontSize: 24, fontWeight: 600, letterSpacing: '-.02em', fontVariantNumeric: 'tabular-nums', marginTop: 4, color: accent ?? 'inherit' }}>{v}</div>
      <div style={{ fontSize: 'var(--fs-xs)', color: solid ? 'rgba(255,255,255,.75)' : 'var(--muted)', marginTop: 4 }}>{sub}</div>
    </div>
  )
}

function OverviewTab({ contract, onAdjust }: { contract: ContractDto; onAdjust: () => void }) {
  const adjLabel = contract.adjustmentType === 'ICL' ? 'ICL' : contract.adjustmentType === 'IPC' ? 'IPC' : 'Manual'
  const freqLabel = contract.adjustmentFrequency === 'Monthly' ? 'mensual' : contract.adjustmentFrequency === 'Quarterly' ? 'trimestral' : 'anual'

  // Real projection from indices-api (null for Manual contracts).
  const { data: projection } = useAdjustmentProjection(contract.id)
  const lastAdjusted = projection?.schedule.filter((s) => s.indexAvailable && s.coefficient != null).at(-1)
  const baseRent = projection?.schedule[0]?.rent ?? contract.monthlyRent
  const coeffText = lastAdjusted?.coefficient != null ? lastAdjusted.coefficient.toFixed(4) : '—'
  const coeffSub = lastAdjusted?.coefficient != null
    ? `+${lastAdjusted.variationPct?.toFixed(2)}%`
    : (contract.adjustmentType === 'Manual' ? 'ajuste manual' : 'pendiente de índice')
  const projectedText = projection ? formatARS(projection.currentRent) : '—'
  const projectedSub = projection
    ? `vigente · ${lastAdjusted ? `desde ${lastAdjusted.from}` : 'sin ajustes aún'}`
    : (contract.adjustmentType === 'Manual' ? 'definido manualmente' : 'requiere índice actual')

  return (
    <>
      <div style={{ display: 'grid', gridTemplateColumns: '1.5fr 1fr', gap: 'var(--s-7)' }}>
        <div className="card">
          <div className="card-h">
            <div>
              <h3>Próximo ajuste — cómo se calcula</h3>
              <div className="sub">{adjLabel} · {freqLabel} · auto-aplicable</div>
            </div>
            <div className="row">
              <button className="btn btn--sm" onClick={onAdjust}><IcEdit size={12} /> Ajuste manual</button>
              <button className="btn btn--sm btn--primary" onClick={onAdjust}>Aplicar ahora <IcChev size={12} /></button>
            </div>
          </div>
          <div className="card-b" style={{ display: 'flex', flexDirection: 'column', gap: 18 }}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr auto 1fr auto 1fr', alignItems: 'center', gap: 12 }}>
              <CalcCell k="Base" v={formatARS(baseRent)} sub={contract.startDate} />
              <div style={{ fontSize: 22, color: 'var(--muted)', textAlign: 'center', fontWeight: 300 }}>×</div>
              <CalcCell k="Coeficiente" v={coeffText} sub={coeffSub} accent={lastAdjusted ? undefined : 'var(--muted)'} />
              <div style={{ fontSize: 22, color: 'var(--muted)', textAlign: 'center', fontWeight: 300 }}>=</div>
              <CalcCell
                k="Alquiler proyectado"
                v={projectedText}
                sub={<span style={{ color: 'rgba(255,255,255,.75)' }}>{projectedSub}</span>}
                solid
              />
            </div>

            <div className="between" style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>
              <span style={{ display: 'inline-flex', gap: 6, alignItems: 'center' }}>
                <IcShield size={12} /> Datos verificados con BCRA
              </span>
              <span style={{ display: 'inline-flex', gap: 6, alignItems: 'center' }}>
                <IcBell size={12} /> Inquilino será notificado por email
              </span>
            </div>
          </div>
        </div>

        <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--s-7)' }}>
          <div className="card card-b" style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
            <div className="sect-title">Partes</div>
            {[
              { name: contract.appTenantFullName, role: 'Inquilino', sub: null },
            ].map((p) => {
              const initials = p.name.split(' ').map((s) => s[0]).slice(0, 2).join('')
              return (
                <div key={p.name} className="row" style={{ gap: 10 }}>
                  <div className="mono-avatar">{initials}</div>
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div className="row" style={{ gap: 6 }}>
                      <span style={{ fontSize: 'var(--fs-sm)', fontWeight: 500 }}>{p.name}</span>
                      <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>· {p.role}</span>
                    </div>
                  </div>
                </div>
              )
            })}
            <div className="hr" style={{ margin: 0 }} />
            <div className="sect-title">Términos</div>
            {[
              { k: 'Inicio', v: formatDate(contract.startDate) },
              { k: 'Fin', v: formatDate(contract.endDate) },
              { k: 'Depósito', v: contract.depositAmount != null ? formatARS(contract.depositAmount) : '—' },
              { k: 'Indexación', v: `${adjLabel} · ${freqLabel}` },
              { k: 'Día de pago', v: `día ${contract.dayOfMonth}` },
              { k: 'Moneda', v: contract.currency },
            ].map((kv) => (
              <div key={kv.k} className="between">
                <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>{kv.k}</span>
                <span style={{ fontSize: 'var(--fs-sm)', fontWeight: 500 }}>{kv.v}</span>
              </div>
            ))}
            {contract.notes && (
              <>
                <div className="hr" style={{ margin: 0 }} />
                <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', lineHeight: 1.5 }}>{contract.notes}</div>
              </>
            )}
          </div>
        </div>
      </div>
    </>
  )
}

function PaymentsTab({ contractId }: { contractId: string }) {
  const { data: transactions, isLoading } = useTransactions(contractId)
  const registerPayment = useRegisterPayment()
  const [showForm, setShowForm] = useState(false)
  const [amount, setAmount] = useState('')
  const [period, setPeriod] = useState('')
  const [notes, setNotes] = useState('')
  const [err, setErr] = useState<string | null>(null)

  const payments = (transactions ?? []).filter(t =>
    t.type === 'Payment' || t.type === 'RentCharge'
  )

  async function handleRegister() {
    setErr(null)
    if (!amount || !period) { setErr('Importe y período son obligatorios'); return }
    try {
      await registerPayment.mutateAsync({
        contractId,
        req: { amount: parseFloat(amount), period, notes: notes || undefined },
      })
      setShowForm(false)
      setAmount(''); setPeriod(''); setNotes('')
    } catch {
      setErr('Error al registrar pago')
    }
  }

  return (
    <>
      <div className="between" style={{ marginBottom: 'var(--s-7)' }}>
        <div className="row">
          <button className="btn btn--sm"><IcCalendar size={12} /> Todos</button>
          <button className="btn btn--sm"><IcDownload size={12} /> Exportar</button>
        </div>
        <button className="btn btn--sm btn--primary" onClick={() => setShowForm(v => !v)}>
          <IcPlus size={12} /> Registrar pago
        </button>
      </div>

      {showForm && (
        <div className="card card-b" style={{ marginBottom: 'var(--s-7)', display: 'flex', flexDirection: 'column', gap: 12 }}>
          <div style={{ fontWeight: 500, fontSize: 'var(--fs-sm)' }}>Registrar pago</div>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 2fr', gap: 12 }}>
            <div>
              <label style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', display: 'block', marginBottom: 4 }}>Importe</label>
              <input className="inp" type="number" value={amount} onChange={e => setAmount(e.target.value)} placeholder="200000" />
            </div>
            <div>
              <label style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', display: 'block', marginBottom: 4 }}>Período</label>
              <input className="inp" type="date" value={period} onChange={e => setPeriod(e.target.value)} />
            </div>
            <div>
              <label style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', display: 'block', marginBottom: 4 }}>Notas (opcional)</label>
              <input className="inp" type="text" value={notes} onChange={e => setNotes(e.target.value)} placeholder="Transferencia XXXX" />
            </div>
          </div>
          {err && <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--danger)' }}>{err}</div>}
          <div className="row">
            <button className="btn btn--sm" onClick={() => setShowForm(false)}>Cancelar</button>
            <button className="btn btn--sm btn--primary" onClick={handleRegister} disabled={registerPayment.isPending}>
              {registerPayment.isPending ? 'Guardando…' : 'Guardar'}
            </button>
          </div>
        </div>
      )}

      <div className="card" style={{ overflow: 'hidden' }}>
        {isLoading ? (
          <div style={{ padding: 24, textAlign: 'center', color: 'var(--muted)' }}>Cargando…</div>
        ) : payments.length === 0 ? (
          <div style={{ padding: 24, textAlign: 'center', color: 'var(--muted)' }}>Sin transacciones registradas</div>
        ) : (
          <table className="tbl">
            <thead>
              <tr>
                <th>Período</th>
                <th>Tipo</th>
                <th className="num">Importe</th>
                <th>Moneda</th>
                <th>Notas</th>
                <th>Fecha</th>
              </tr>
            </thead>
            <tbody>
              {payments.map((t) => (
                <tr key={t.id}>
                  <td><b>{formatDate(t.period)}</b></td>
                  <td>
                    {t.type === 'Payment'
                      ? <span className="chip chip--ok"><span className="dot" />Pago</span>
                      : <span className="chip"><span className="dot" />Cargo alquiler</span>
                    }
                  </td>
                  <td className="num"><b>{formatARS(t.amount)}</b></td>
                  <td>{t.currency}</td>
                  <td style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>{t.notes ?? '—'}</td>
                  <td className="muted">{formatDate(t.createdAt.split('T')[0])}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </>
  )
}

function AdjustmentsTab({ contractId }: { contractId: string }) {
  const { data: history, isLoading } = useRentHistory(contractId)
  const applyAdj = useApplyAdjustment()
  const [showForm, setShowForm] = useState(false)
  const [newRent, setNewRent] = useState('')
  const [adjNotes, setAdjNotes] = useState('')
  const [adjDate, setAdjDate] = useState('')
  const [err, setErr] = useState<string | null>(null)

  async function handleApply() {
    setErr(null)
    if (!newRent || !adjNotes) { setErr('Nuevo valor y nota son obligatorios para ajuste manual'); return }
    try {
      await applyAdj.mutateAsync({
        contractId,
        req: {
          effectiveDate: adjDate || undefined,
          manualNewRent: parseFloat(newRent),
          notes: adjNotes,
        },
      })
      setShowForm(false)
      setNewRent(''); setAdjNotes(''); setAdjDate('')
    } catch {
      setErr('Error al aplicar ajuste')
    }
  }

  return (
    <>
      <div className="between" style={{ marginBottom: 'var(--s-7)' }}>
        <div style={{ fontSize: 'var(--fs-sm)', fontWeight: 500 }}>
          {history?.length ?? 0} ajustes registrados
        </div>
        <button className="btn btn--sm btn--primary" onClick={() => setShowForm(v => !v)}>
          <IcEdit size={12} /> Ajuste manual
        </button>
      </div>

      {showForm && (
        <div className="card card-b" style={{ marginBottom: 'var(--s-7)', display: 'flex', flexDirection: 'column', gap: 12 }}>
          <div style={{ fontWeight: 500, fontSize: 'var(--fs-sm)' }}>Aplicar ajuste manual</div>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 2fr', gap: 12 }}>
            <div>
              <label style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', display: 'block', marginBottom: 4 }}>Nuevo alquiler</label>
              <input className="inp" type="number" value={newRent} onChange={e => setNewRent(e.target.value)} placeholder="250000" />
            </div>
            <div>
              <label style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', display: 'block', marginBottom: 4 }}>Fecha efectiva</label>
              <input className="inp" type="date" value={adjDate} onChange={e => setAdjDate(e.target.value)} />
            </div>
            <div>
              <label style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', display: 'block', marginBottom: 4 }}>Nota (obligatoria)</label>
              <input className="inp" type="text" value={adjNotes} onChange={e => setAdjNotes(e.target.value)} placeholder="Acuerdo entre partes…" />
            </div>
          </div>
          {err && <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--danger)' }}>{err}</div>}
          <div className="row">
            <button className="btn btn--sm" onClick={() => setShowForm(false)}>Cancelar</button>
            <button className="btn btn--sm btn--primary" onClick={handleApply} disabled={applyAdj.isPending}>
              {applyAdj.isPending ? 'Aplicando…' : 'Aplicar'}
            </button>
          </div>
        </div>
      )}

      <div className="card">
        {isLoading ? (
          <div style={{ padding: 24, textAlign: 'center', color: 'var(--muted)' }}>Cargando…</div>
        ) : !history || history.length === 0 ? (
          <div style={{ padding: 24, textAlign: 'center', color: 'var(--muted)' }}>Sin ajustes registrados</div>
        ) : (
          <table className="tbl">
            <thead>
              <tr>
                <th>Fecha efectiva</th>
                <th>Tipo</th>
                <th>Coeficiente</th>
                <th className="num">Anterior</th>
                <th className="num">Nuevo</th>
                <th className="num">Δ</th>
                <th>Notas</th>
              </tr>
            </thead>
            <tbody>
              {history.map((a) => {
                const pct = ((a.newRent - a.previousRent) / a.previousRent) * 100
                return (
                  <tr key={a.id}>
                    <td><b>{formatDate(a.effectiveDate)}</b></td>
                    <td>
                      <span className={`chip chip--${a.adjustmentType === 'ICL' ? 'icl' : a.adjustmentType === 'IPC' ? 'ipc' : 'warn'}`}>
                        <span className="dot" />{a.adjustmentType}
                      </span>
                    </td>
                    <td className="mono tnum">{a.adjustmentFactor.toFixed(4)}</td>
                    <td className="num">{formatARS(a.previousRent)}</td>
                    <td className="num"><b>{formatARS(a.newRent)}</b></td>
                    <td className="num">
                      <span className="delta-pill up"><IcArrowUp size={10} />+{pct.toFixed(2)}%</span>
                    </td>
                    <td style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>{a.notes ?? '—'}</td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        )}
      </div>
    </>
  )
}

function DocumentsTab({ contractId }: { contractId: string }) {
  const { data: docs, isLoading } = useContractDocuments(contractId)
  const upload = useUploadDocument(contractId)
  const deleteDoc = useDeleteDocument(contractId)
  const fileRef = useRef<HTMLInputElement>(null)
  const [urlModal, setUrlModal] = useState<DocumentDownloadUrlDto | null>(null)
  const [loadingDocId, setLoadingDocId] = useState<string | null>(null)

  async function handleFile(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    await upload.mutateAsync(file)
    if (fileRef.current) fileRef.current.value = ''
  }

  async function handleGetUrl(docId: string) {
    setLoadingDocId(docId)
    try {
      const dto = await documentService.getDownloadUrl(contractId, docId)
      setUrlModal(dto)
    } finally {
      setLoadingDocId(null)
    }
  }

  function fmtBytes(b: number) {
    if (b < 1024) return `${b} B`
    if (b < 1024 * 1024) return `${(b / 1024).toFixed(1)} KB`
    return `${(b / (1024 * 1024)).toFixed(1)} MB`
  }

  return (
    <>
      <div className="between" style={{ marginBottom: 'var(--s-7)' }}>
        <div style={{ fontSize: 'var(--fs-sm)', fontWeight: 500 }}>
          {docs?.length ?? 0} documento{(docs?.length ?? 0) !== 1 ? 's' : ''}
        </div>
        <div className="row">
          <input type="file" ref={fileRef} style={{ display: 'none' }} onChange={handleFile} />
          <button className="btn btn--sm btn--primary" onClick={() => fileRef.current?.click()} disabled={upload.isPending}>
            <IcUpload size={12} /> {upload.isPending ? 'Subiendo…' : 'Subir'}
          </button>
        </div>
      </div>

      <div className="card" style={{ overflow: 'hidden' }}>
        {isLoading ? (
          <div style={{ padding: 24, textAlign: 'center', color: 'var(--muted)' }}>Cargando…</div>
        ) : !docs || docs.length === 0 ? (
          <div style={{ padding: 32, textAlign: 'center', color: 'var(--muted)' }}>
            <IcDoc size={28} style={{ opacity: .3, display: 'block', margin: '0 auto 8px' }} />
            <div>Sin documentos adjuntos</div>
            <div style={{ fontSize: 'var(--fs-xs)', marginTop: 4 }}>Los archivos se sirven con URLs pre-firmadas de 5 min</div>
          </div>
        ) : (
          <table className="tbl">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Tipo</th>
                <th className="num">Tamaño</th>
                <th>Fecha</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {docs.map((d) => (
                <tr key={d.id}>
                  <td>
                    <div className="row" style={{ gap: 8 }}>
                      <IcDoc size={14} style={{ color: 'var(--muted)', flexShrink: 0 }} />
                      <b style={{ fontSize: 'var(--fs-sm)' }}>{d.fileName}</b>
                    </div>
                  </td>
                  <td style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>{d.mimeType}</td>
                  <td className="num tnum" style={{ fontSize: 'var(--fs-xs)' }}>{fmtBytes(d.sizeBytes)}</td>
                  <td className="muted">{formatDate(d.createdAt.split('T')[0])}</td>
                  <td>
                    <div className="row" style={{ justifyContent: 'flex-end', gap: 4 }}>
                      <button
                        className="btn btn--ghost btn--sm btn--icon"
                        title="Obtener link de descarga"
                        aria-label="Obtener link de descarga"
                        onClick={() => handleGetUrl(d.id)}
                        disabled={loadingDocId === d.id}
                      >
                        <IcDownload size={13} />
                      </button>
                      <button
                        className="btn btn--ghost btn--sm btn--icon"
                        style={{ color: 'var(--danger)' }}
                        title="Eliminar documento"
                        aria-label="Eliminar documento"
                        onClick={() => {
                          if (window.confirm('¿Eliminar este documento? Esta acción no se puede deshacer.'))
                            deleteDoc.mutate(d.id)
                        }}
                      >
                        ×
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {urlModal && (
        <div
          onClick={() => setUrlModal(null)}
          style={{ position: 'fixed', inset: 0, background: 'rgba(20,20,16,.45)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 300 }}
        >
          <div onClick={e => e.stopPropagation()} className="card" style={{ width: 420, padding: 20, display: 'flex', flexDirection: 'column', gap: 12 }}>
            <div className="row">
              <div style={{ width: 36, height: 36, borderRadius: 9, background: 'var(--brand-50)', color: 'var(--brand)', display: 'grid', placeItems: 'center' }}>
                <IcShield size={18} />
              </div>
              <div>
                <div style={{ fontWeight: 600, fontSize: 'var(--fs-sm)' }}>Link seguro generado</div>
                <div style={{ fontSize: 11, color: 'var(--muted)' }}>Expira a las {new Date(urlModal.expiresAt).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' })}</div>
              </div>
            </div>
            <div style={{ background: 'var(--surface-2)', border: '1px solid var(--hairline)', borderRadius: 8, padding: '10px 12px', fontFamily: 'var(--font-mono)', fontSize: 11, color: 'var(--ink-soft)', wordBreak: 'break-all' }}>
              {urlModal.url}
            </div>
            <div className="row" style={{ gap: 6 }}>
              <button className="btn" style={{ flex: 1, justifyContent: 'center' }} onClick={() => navigator.clipboard.writeText(urlModal.url)}>
                <IcLink size={13} /> Copiar
              </button>
              <button className="btn btn--primary" style={{ flex: 1, justifyContent: 'center' }} onClick={() => window.open(urlModal.url, '_blank')}>
                <IcDownload size={13} /> Abrir
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  )
}

export default function ContratoDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [tab, setTab] = useState<TabKey>('overview')
  const { data: contract, isLoading } = useContractById(id)

  const statusLabel = contract?.status === 'Active' ? 'Vigente' : contract?.status === 'Terminated' ? 'Rescindido' : 'Expirado'
  const statusChip = contract?.status === 'Active' ? 'ok' : contract?.status === 'Terminated' ? 'danger' : 'warn'

  return (
    <>
      <AdminTopbar
        crumbs={['Contratos', id ?? '…']}
        right={
          <div className="row">
            <button className="btn btn--sm"><IcEdit size={12} /> Editar</button>
            <button className="btn btn--sm btn--icon"><IcEllipsis size={12} /></button>
          </div>
        }
      />
      <div className="page" style={{ padding: 0, gap: 0 }}>
        <div style={{ padding: 'var(--s-9) var(--s-9) var(--s-7)', background: 'var(--surface)', borderBottom: '1px solid var(--hairline)' }}>
          {isLoading || !contract ? (
            <div style={{ color: 'var(--muted)', fontSize: 'var(--fs-sm)' }}>Cargando contrato…</div>
          ) : (
            <>
              <div className="between" style={{ alignItems: 'flex-start' }}>
                <div>
                  <div className="row" style={{ marginBottom: 6 }}>
                    <span className={`chip chip--${statusChip}`}><span className="dot" />{statusLabel}</span>
                    {contract.adjustmentType !== 'Manual' && (
                      <span className={`chip chip--${contract.adjustmentType === 'ICL' ? 'icl' : 'ipc'}`}>
                        <span className="dot" />{contract.adjustmentType} · {contract.adjustmentFrequency === 'Monthly' ? 'mensual' : contract.adjustmentFrequency === 'Quarterly' ? 'trim.' : 'anual'}
                      </span>
                    )}
                    <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>
                      Inicio {formatDate(contract.startDate)} · Fin {formatDate(contract.endDate)}
                    </span>
                  </div>
                  <h1 style={{ margin: 0, fontSize: 26, fontWeight: 600, letterSpacing: '-.02em' }}>
                    {contract.appTenantFullName}
                  </h1>
                  <div className="row muted" style={{ marginTop: 4, fontSize: 'var(--fs-sm)' }}>
                    <IcPin size={14} /> {contract.propertyAddress}, {contract.propertyCity}
                  </div>
                </div>
                <div className="row" style={{ alignItems: 'stretch' }}>
                  <div className="card" style={{ padding: '10px 14px', display: 'flex', flexDirection: 'column', gap: 2 }}>
                    <span className="label">Alquiler vigente</span>
                    <div style={{ fontSize: 20, fontWeight: 600, fontVariantNumeric: 'tabular-nums', letterSpacing: '-.01em' }}>
                      {formatARS(contract.monthlyRent)}
                    </div>
                  </div>
                  <div className="card" style={{ padding: '10px 14px', display: 'flex', flexDirection: 'column', gap: 2, background: 'var(--brand-50)', borderColor: 'var(--brand-100)' }}>
                    <span className="label" style={{ color: 'var(--brand-700)' }}>Moneda</span>
                    <div style={{ fontSize: 20, fontWeight: 600, letterSpacing: '-.01em', color: 'var(--brand-700)' }}>
                      {contract.currency}
                    </div>
                  </div>
                </div>
              </div>

              <div style={{ marginTop: 18, display: 'flex', gap: 4 }}>
                {TABS.map((t) => (
                  <button
                    key={t.k}
                    onClick={() => setTab(t.k)}
                    style={{
                      padding: '8px 12px', borderRadius: 6, border: 'none',
                      background: tab === t.k ? 'var(--surface-3)' : 'transparent',
                      color: tab === t.k ? 'var(--ink)' : 'var(--muted)',
                      fontWeight: 500, fontSize: 'var(--fs-sm)', cursor: 'pointer',
                      fontFamily: 'inherit',
                    }}
                  >
                    {t.lbl}
                  </button>
                ))}
              </div>
            </>
          )}
        </div>

        <div className="page" style={{ padding: 'var(--s-9)', flex: 1 }}>
          {!isLoading && contract && (
            <>
              {tab === 'overview' && <OverviewTab contract={contract} onAdjust={() => setTab('adjustments')} />}
              {tab === 'payments' && <PaymentsTab contractId={contract.id} />}
              {tab === 'adjustments' && <AdjustmentsTab contractId={contract.id} />}
              {tab === 'documents' && <DocumentsTab contractId={contract.id} />}
            </>
          )}
        </div>
      </div>
    </>
  )
}
