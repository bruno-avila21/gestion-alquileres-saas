import type { AdjustmentType, AdjustmentFrequency, ContractCurrency } from '../types/contract.types'
import type { PropertyDto } from '@/features/properties/types/property.types'
import type { AppTenantDto } from '@/features/apptenants/types/apptenant.types'
import { FREQ_OPTIONS, type ContractFormState } from '../utils/contractForm'

/**
 * Los campos de un contrato, compartidos por el alta y la edición. El estado vive en el
 * padre porque cada pantalla lo inicializa distinto (vacío / desde el contrato) y decide
 * qué hacer al guardar. El modelo está en `../utils/contractForm`.
 */

type Props = {
  form: ContractFormState
  setForm: React.Dispatch<React.SetStateAction<ContractFormState>>
  properties: PropertyDto[] | undefined
  tenants: AppTenantDto[] | undefined
  /** Prefijo de los `id` para que dos formularios en la misma página no compartan etiqueta. */
  idPrefix?: string
}

export function ContratoFormFields({ form, setForm, properties, tenants, idPrefix = '' }: Props) {
  const id = (name: string) => `${idPrefix}${name}`

  /* Se ofrecen los activos MÁS el que el contrato ya tiene, aunque esté dado de baja. Si se
     filtrara sólo por activos, editar un contrato cuya propiedad se desactivó dejaría el
     select en "Seleccioná una propiedad" y guardar lo reasignaría a otra sin que se note. */
  const propsElegibles = (properties ?? []).filter(p => p.isActive || p.id === form.propertyId)
  const tenantsElegibles = (tenants ?? []).filter(t => t.isActive || t.id === form.appTenantId)

  return (
    <>
      <div className="grid-2">
        <div>
          <label className="label" htmlFor={id('propertyId')}>Propiedad *</label>
          <select
            id={id('propertyId')}
            className="select"
            value={form.propertyId}
            onChange={e => setForm(f => ({ ...f, propertyId: e.target.value }))}
            required
          >
            <option value="">Seleccioná una propiedad</option>
            {propsElegibles.map(p => (
              <option key={p.id} value={p.id}>{p.address} · {p.city}</option>
            ))}
          </select>
        </div>
        <div>
          <label className="label" htmlFor={id('appTenantId')}>Inquilino *</label>
          <select
            id={id('appTenantId')}
            className="select"
            value={form.appTenantId}
            onChange={e => setForm(f => ({ ...f, appTenantId: e.target.value }))}
            required
          >
            <option value="">Seleccioná un inquilino</option>
            {tenantsElegibles.map(t => (
              <option key={t.id} value={t.id}>{t.firstName} {t.lastName} · DNI {t.dni}</option>
            ))}
          </select>
        </div>
      </div>

      <div className="grid-2">
        <div>
          <label className="label" htmlFor={id('startDate')}>Inicio *</label>
          <input
            id={id('startDate')}
            className="input"
            type="date"
            value={form.startDate}
            onChange={e => setForm(f => ({ ...f, startDate: e.target.value }))}
            required
          />
        </div>
        <div>
          <label className="label" htmlFor={id('endDate')}>Fin *</label>
          <input
            id={id('endDate')}
            className="input"
            type="date"
            value={form.endDate}
            onChange={e => setForm(f => ({ ...f, endDate: e.target.value }))}
            required
          />
        </div>
      </div>

      <div className="grid-3">
        <div>
          <label className="label" htmlFor={id('monthlyRent')}>Alquiler mensual *</label>
          <input
            id={id('monthlyRent')}
            className="input"
            type="number"
            min="1"
            step="0.01"
            value={form.monthlyRent}
            onChange={e => setForm(f => ({ ...f, monthlyRent: e.target.value }))}
            required
          />
        </div>
        <div>
          <label className="label" htmlFor={id('currency')}>Moneda *</label>
          <select
            id={id('currency')}
            className="select"
            value={form.currency}
            onChange={e => setForm(f => ({ ...f, currency: e.target.value as ContractCurrency }))}
          >
            <option value="ARS">ARS</option>
            <option value="USD">USD</option>
          </select>
        </div>
        <div>
          <label className="label" htmlFor={id('depositAmount')}>Depósito</label>
          <input
            id={id('depositAmount')}
            className="input"
            type="number"
            min="0"
            step="0.01"
            value={form.depositAmount}
            onChange={e => setForm(f => ({ ...f, depositAmount: e.target.value }))}
          />
        </div>
      </div>

      <div className="grid-3">
        <div>
          <label className="label" htmlFor={id('adjustmentType')}>Tipo de ajuste *</label>
          <select
            id={id('adjustmentType')}
            className="select"
            value={form.adjustmentType}
            onChange={e => setForm(f => ({
              ...f,
              adjustmentType: e.target.value as AdjustmentType,
              // El backend exige que el porcentaje vaya vacío si el tipo no es % fijo.
              adjustmentPercent: e.target.value === 'FixedPercent' ? f.adjustmentPercent : '',
            }))}
          >
            <option value="ICL">ICL — Contratos de Locación (BCRA)</option>
            <option value="IPC">IPC — Precios al Consumidor (INDEC)</option>
            <option value="FixedPercent">% fijo pactado</option>
            <option value="Manual">Manual</option>
          </select>
        </div>
        <div>
          <label className="label" htmlFor={id('adjustmentFrequency')}>Frecuencia *</label>
          <select
            id={id('adjustmentFrequency')}
            className="select"
            value={form.adjustmentFrequency}
            onChange={e => setForm(f => ({ ...f, adjustmentFrequency: e.target.value as AdjustmentFrequency }))}
          >
            {FREQ_OPTIONS.map(o => (
              <option key={o.value} value={o.value}>{o.label}</option>
            ))}
          </select>
        </div>
        {form.adjustmentType === 'FixedPercent' && (
          <div>
            <label className="label" htmlFor={id('adjustmentPercent')}>Porcentaje *</label>
            <input
              id={id('adjustmentPercent')}
              className="input"
              type="number"
              inputMode="decimal"
              step="0.001"
              min="0"
              placeholder="8"
              value={form.adjustmentPercent}
              onChange={e => setForm(f => ({ ...f, adjustmentPercent: e.target.value }))}
              required
            />
            <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', marginTop: 4 }}>
              Se aplica en cada período. Ej: 8 para un 8% {(
                FREQ_OPTIONS.find(o => o.value === form.adjustmentFrequency)?.label ?? ''
              ).toLowerCase()}.
            </div>
          </div>
        )}
        <div>
          <label className="label" htmlFor={id('dayOfMonth')}>Día de cobro *</label>
          <input
            id={id('dayOfMonth')}
            className="input"
            type="number"
            min="1"
            max="28"
            value={form.dayOfMonth}
            onChange={e => setForm(f => ({ ...f, dayOfMonth: e.target.value }))}
            required
          />
        </div>
        <div>
          <label className="label" htmlFor={id('lateFeeDailyRate')}>Punitorio diario</label>
          <input
            id={id('lateFeeDailyRate')}
            className="input"
            type="number"
            inputMode="decimal"
            step="0.0001"
            min="0"
            placeholder="0,1"
            value={form.lateFeeDailyRate}
            onChange={e => setForm(f => ({ ...f, lateFeeDailyRate: e.target.value }))}
          />
          <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', marginTop: 4 }}>
            % por día sobre lo impago. Vacío = el contrato no pactó punitorio.
          </div>
        </div>
        <div>
          <label className="label" htmlFor={id('lateFeeGraceDays')}>Días de gracia</label>
          <input
            id={id('lateFeeGraceDays')}
            className="input"
            type="number"
            min="0"
            max="90"
            value={form.lateFeeGraceDays}
            onChange={e => setForm(f => ({ ...f, lateFeeGraceDays: e.target.value }))}
            disabled={!form.lateFeeDailyRate}
          />
          <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', marginTop: 4 }}>
            Tolerancia desde el vencimiento antes de que empiece a correr.
          </div>
        </div>
      </div>

      <div>
        <label className="label" htmlFor={id('notes')}>Notas</label>
        <input
          id={id('notes')}
          className="input"
          value={form.notes}
          onChange={e => setForm(f => ({ ...f, notes: e.target.value }))}
        />
      </div>
    </>
  )
}
