import { useState } from 'react'
import { formatARSExact } from '@/shared/lib/formatters'
import { useSimulateAdjustment } from '../hooks/useOwners'
import type { SimulateAdjustmentParams } from '../types/owner.types'

const TIPOS: { value: SimulateAdjustmentParams['type']; label: string }[] = [
  { value: 'ICL', label: 'ICL (BCRA)' },
  { value: 'IPC', label: 'IPC (INDEC)' },
  { value: 'FixedPercent', label: 'Porcentaje fijo' },
]

const FRECUENCIAS: { value: SimulateAdjustmentParams['frequency']; label: string }[] = [
  { value: 'Monthly', label: 'Mensual' },
  { value: 'Quarterly', label: 'Trimestral' },
  { value: 'FourMonthly', label: 'Cuatrimestral' },
  { value: 'SemiAnnual', label: 'Semestral' },
  { value: 'Annual', label: 'Anual' },
]

/**
 * Calculadora de ajuste antes de firmar.
 *
 * No toca ningún contrato ni escribe nada: contesta la pregunta que hoy la inmobiliaria
 * resuelve en una planilla aparte —"si pacto ICL trimestral sobre $ 500.000 desde marzo,
 * ¿en cuánto termina?"— usando el mismo motor que después aplica los ajustes de verdad.
 * Que sea el mismo motor es el punto: una planilla y el sistema pueden dar distinto.
 */
export function SimuladorAjusteModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const simular = useSimulateAdjustment()

  const [tipo, setTipo] = useState<SimulateAdjustmentParams['type']>('ICL')
  const [monto, setMonto] = useState('500000')
  const [desde, setDesde] = useState(() => new Date().toISOString().slice(0, 10))
  const [frecuencia, setFrecuencia] = useState<SimulateAdjustmentParams['frequency']>('Quarterly')
  const [pct, setPct] = useState('8')

  if (!open) return null

  const importe = Number(monto.replace(/\D/g, ''))
  const puedeSimular = importe > 0 && !!desde && (tipo !== 'FixedPercent' || Number(pct) > 0)

  function handleSimular() {
    if (!puedeSimular) return
    simular.mutate({
      type: tipo,
      initialRent: importe,
      startDate: desde,
      frequency: frecuencia,
      ...(tipo === 'FixedPercent' ? { percent: Number(pct) } : {}),
    })
  }

  const proyeccion = simular.data
  const cuotas = proyeccion?.schedule ?? []

  return (
    <div className="modal-backdrop" role="dialog" aria-modal="true" aria-label="Simular ajuste">
      <div className="modal" style={{ maxWidth: 720 }}>
        <div className="modal-h">
          <h3>Simular ajuste ICL / IPC</h3>
          <button className="btn btn--ghost btn--icon btn--sm" onClick={onClose} aria-label="Cerrar">✕</button>
        </div>

        <div className="modal-b" style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          <div style={{ fontSize: 'var(--fs-sm)', color: 'var(--muted)', lineHeight: 1.5 }}>
            Usa el mismo motor que aplica los ajustes reales, con los índices del BCRA y el INDEC.
            No modifica ningún contrato.
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 14 }}>
            <div>
              <label className="label" htmlFor="sim-tipo">Tipo de ajuste</label>
              <select
                id="sim-tipo" className="select" style={{ marginTop: 4 }}
                value={tipo} onChange={(e) => setTipo(e.target.value as SimulateAdjustmentParams['type'])}
              >
                {TIPOS.map((t) => <option key={t.value} value={t.value}>{t.label}</option>)}
              </select>
            </div>
            <div>
              <label className="label" htmlFor="sim-frec">Frecuencia</label>
              <select
                id="sim-frec" className="select" style={{ marginTop: 4 }}
                value={frecuencia}
                onChange={(e) => setFrecuencia(e.target.value as SimulateAdjustmentParams['frequency'])}
              >
                {FRECUENCIAS.map((f) => <option key={f.value} value={f.value}>{f.label}</option>)}
              </select>
            </div>
            <div>
              <label className="label" htmlFor="sim-monto">Alquiler inicial</label>
              <input
                id="sim-monto" className="input" inputMode="numeric" style={{ marginTop: 4 }}
                value={monto} onChange={(e) => setMonto(e.target.value)}
              />
            </div>
            <div>
              <label className="label" htmlFor="sim-desde">Desde</label>
              <input
                id="sim-desde" className="input" type="date" style={{ marginTop: 4 }}
                value={desde} onChange={(e) => setDesde(e.target.value)}
              />
            </div>
            {tipo === 'FixedPercent' ? (
              <div>
                <label className="label" htmlFor="sim-pct">Porcentaje por período</label>
                <input
                  id="sim-pct" className="input" inputMode="decimal" style={{ marginTop: 4 }}
                  value={pct} onChange={(e) => setPct(e.target.value)}
                />
              </div>
            ) : null}
          </div>

          <button
            className="btn btn--primary"
            style={{ alignSelf: 'flex-start' }}
            disabled={!puedeSimular || simular.isPending}
            onClick={handleSimular}
          >
            {simular.isPending ? 'Calculando…' : 'Calcular'}
          </button>

          {simular.isError ? (
            <div role="alert" style={{ fontSize: 'var(--fs-sm)', color: 'var(--danger)' }}>
              No pudimos calcular la proyección. Si es ICL o IPC, puede que el índice no esté
              disponible para ese período.
            </div>
          ) : null}

          {simular.isSuccess && cuotas.length === 0 ? (
            <div style={{ fontSize: 'var(--fs-sm)', color: 'var(--muted)' }}>
              Ese tipo de ajuste no tiene fórmula que proyectar.
            </div>
          ) : null}

          {cuotas.length > 0 ? (
            <>
              <table className="tbl">
                <thead>
                  <tr>
                    <th>Período</th>
                    <th>Desde</th>
                    <th className="num">Variación</th>
                    <th className="num">Alquiler</th>
                  </tr>
                </thead>
                <tbody>
                  {cuotas.map((c) => (
                    <tr key={c.number}>
                      <td>{c.number}</td>
                      <td className="muted">{c.from}</td>
                      <td className="num muted">
                        {c.variationPct === null ? '—' : `${c.variationPct > 0 ? '+' : ''}${c.variationPct}%`}
                      </td>
                      <td className="num">
                        {c.indexAvailable && c.rent !== null
                          ? <b>{formatARSExact(c.rent)}</b>
                          : <span className="muted">Índice no publicado</span>}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {proyeccion?.notes ? (
                <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>{proyeccion.notes}</div>
              ) : null}
            </>
          ) : null}
        </div>
      </div>
    </div>
  )
}
