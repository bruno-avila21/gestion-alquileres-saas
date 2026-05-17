import { useEffect, useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useSyncIndex } from '../hooks/useSyncIndex'
import type { IndexType, SyncIndexResult } from '../types/index.types'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'

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

  return (
    <dialog
      ref={dialogRef}
      onClose={handleClose}
      className="rounded-lg shadow-xl p-6 w-full max-w-md backdrop:bg-black/50"
    >
      <h2 className="text-lg font-semibold mb-4">Sincronizar Índice</h2>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <Label htmlFor="indexType">Tipo de índice</Label>
          <select
            id="indexType"
            {...register('indexType')}
            className="mt-1 block w-full border border-slate-300 rounded px-3 py-2 text-sm"
          >
            <option value="ICL">ICL</option>
            <option value="IPC">IPC</option>
          </select>
          {errors.indexType && (
            <p role="alert" className="text-red-600 text-xs mt-1">{errors.indexType.message}</p>
          )}
        </div>

        <div className="flex gap-3">
          <div className="flex-1">
            <Label htmlFor="month">Mes (1–12)</Label>
            <Input
              id="month"
              type="number"
              min={1}
              max={12}
              {...register('month')}
            />
            {errors.month && (
              <p role="alert" className="text-red-600 text-xs mt-1">{errors.month.message}</p>
            )}
          </div>
          <div className="flex-1">
            <Label htmlFor="year">Año</Label>
            <Input
              id="year"
              type="number"
              min={2000}
              max={currentYear}
              {...register('year')}
            />
            {errors.year && (
              <p role="alert" className="text-red-600 text-xs mt-1">{errors.year.message}</p>
            )}
          </div>
        </div>

        {errorMsg && (
          <div role="alert" className="text-red-700 bg-red-50 rounded p-3 text-sm">
            {errorMsg}
          </div>
        )}

        {resultMsg && (
          <div
            className={[
              'rounded p-3 text-sm',
              resultMsg.type === 'warning' ? 'bg-yellow-50 text-yellow-800' : '',
              resultMsg.type === 'info' ? 'bg-blue-50 text-blue-800' : '',
              resultMsg.type === 'success' ? 'bg-green-50 text-green-800' : '',
            ].join(' ')}
          >
            {resultMsg.text}
          </div>
        )}

        <div className="flex justify-end gap-2 pt-2">
          <Button type="button" variant="outline" onClick={handleClose} disabled={isPending}>
            Cancelar
          </Button>
          <Button type="submit" disabled={isPending}>
            {isPending ? 'Sincronizando…' : 'Sincronizar'}
          </Button>
        </div>
      </form>
    </dialog>
  )
}
