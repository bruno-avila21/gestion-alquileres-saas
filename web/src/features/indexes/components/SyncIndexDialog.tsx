import { useEffect, useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useSyncIndex } from '../hooks/useSyncIndex'
import type { IndexType, SyncIndexResult } from '../types/index.types'

const currentYear = new Date().getFullYear()

const syncSchema = z.object({
  indexType: z.enum(['ICL', 'IPC'] as const),
  month: z.coerce.number().int().min(1).max(12),
  year: z.coerce.number().int().min(2000).max(currentYear),
})

type SyncFormValues = z.infer<typeof syncSchema>

interface SyncIndexDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  defaultIndexType?: IndexType
}

export function SyncIndexDialog({ open, onOpenChange, defaultIndexType = 'ICL' }: SyncIndexDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const [resultMsg, setResultMsg] = useState<{ type: 'success' | 'warning' | 'info'; text: string } | null>(null)

  const { mutate, isPending, error, reset: resetMutation } = useSyncIndex()

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<SyncFormValues>({
    resolver: zodResolver(syncSchema),
    defaultValues: {
      indexType: defaultIndexType,
      month: new Date().getMonth() + 1,
      year: currentYear,
    },
  })

  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) return
    if (open) {
      if (!dialog.open) dialog.showModal()
    } else {
      if (dialog.open) dialog.close()
    }
  }, [open])

  useEffect(() => {
    if (open) {
      reset({
        indexType: defaultIndexType,
        month: new Date().getMonth() + 1,
        year: currentYear,
      })
      setResultMsg(null)
      resetMutation()
    }
  }, [open, defaultIndexType, reset, resetMutation])

  function handleClose() {
    onOpenChange(false)
  }

  function onSubmit(values: SyncFormValues) {
    const month = String(values.month).padStart(2, '0')
    const period = `${values.year}-${month}-01`
    setResultMsg(null)

    mutate(
      { indexType: values.indexType, period },
      {
        onSuccess: (data: SyncIndexResult) => {
          if (data.wasFallback) {
            setResultMsg({
              type: 'warning',
              text: `Advertencia: API externa no disponible, se usó el último valor disponible (${data.indexValue.period}).`,
            })
          } else if (data.alreadyExisted) {
            setResultMsg({ type: 'info', text: 'Ese período ya estaba sincronizado.' })
          } else {
            setResultMsg({
              type: 'success',
              text: `Sincronización exitosa: ${data.indexValue.indexType} ${data.indexValue.period} = ${data.indexValue.value}`,
            })
          }
          setTimeout(() => {
            onOpenChange(false)
          }, 1500)
        },
      },
    )
  }

  const errorMsg = error instanceof Error ? error.message : null

  const resultColor =
    resultMsg?.type === 'warning' ? 'var(--warn)'
      : resultMsg?.type === 'success' ? 'var(--ok)'
        : 'var(--brand-700)'

  return (
    <dialog
      ref={dialogRef}
      onClose={handleClose}
      className="card"
      style={{ padding: 20, width: '100%', maxWidth: 420, border: 'none', boxShadow: '0 20px 50px rgba(0,0,0,.2)' }}
    >
      <h2 style={{ fontSize: 16, fontWeight: 600, margin: '0 0 16px' }}>Sincronizar índice</h2>

      <form onSubmit={handleSubmit(onSubmit)} style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
        <div>
          <label className="label" htmlFor="indexType">Tipo de índice</label>
          <select id="indexType" className="select" {...register('indexType')}>
            <option value="ICL">ICL</option>
            <option value="IPC">IPC</option>
          </select>
          {errors.indexType && <p role="alert" style={{ fontSize: 'var(--fs-xs)', color: 'var(--danger)', marginTop: 4 }}>{errors.indexType.message}</p>}
        </div>

        <div className="row" style={{ gap: 12 }}>
          <div style={{ flex: 1 }}>
            <label className="label" htmlFor="month">Mes (1–12)</label>
            <input id="month" className="input" type="number" min={1} max={12} {...register('month')} />
            {errors.month && <p role="alert" style={{ fontSize: 'var(--fs-xs)', color: 'var(--danger)', marginTop: 4 }}>{errors.month.message}</p>}
          </div>
          <div style={{ flex: 1 }}>
            <label className="label" htmlFor="year">Año</label>
            <input id="year" className="input" type="number" min={2000} max={currentYear} {...register('year')} />
            {errors.year && <p role="alert" style={{ fontSize: 'var(--fs-xs)', color: 'var(--danger)', marginTop: 4 }}>{errors.year.message}</p>}
          </div>
        </div>

        {errorMsg && (
          <div role="alert" style={{ color: 'var(--danger)', background: 'var(--danger-50)', borderRadius: 8, padding: '10px 12px', fontSize: 'var(--fs-sm)' }}>
            {errorMsg}
          </div>
        )}

        {resultMsg && (
          <div style={{ color: resultColor, background: 'var(--surface-2)', border: '1px solid var(--hairline)', borderRadius: 8, padding: '10px 12px', fontSize: 'var(--fs-sm)' }}>
            {resultMsg.text}
          </div>
        )}

        <div className="row" style={{ justifyContent: 'flex-end', gap: 8, paddingTop: 4 }}>
          <button type="button" className="btn btn--sm" onClick={handleClose} disabled={isPending}>
            Cancelar
          </button>
          <button type="submit" className="btn btn--sm btn--primary" disabled={isPending}>
            {isPending ? 'Sincronizando…' : 'Sincronizar'}
          </button>
        </div>
      </form>
    </dialog>
  )
}
