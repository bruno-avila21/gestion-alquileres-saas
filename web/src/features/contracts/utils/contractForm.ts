import type {
  AdjustmentFrequency,
  AdjustmentType,
  ContractCurrency,
  ContractDto,
  CreateContractRequest,
} from '../types/contract.types'

/**
 * El modelo del formulario de contrato: su estado, sus opciones y la traducción de ida y
 * vuelta con la API. Vive aparte de `ContratoFormFields` porque el lint exige que un
 * archivo con componentes exporte sólo componentes (react-refresh).
 *
 * Lo comparten el alta y la edición: es la misma entidad con las mismas reglas, y tener
 * dos copias garantizaba que el día que se agregue un campo al motor de ajustes una se
 * quedara atrás.
 */

export type ContractFormState = {
  propertyId: string
  appTenantId: string
  startDate: string
  endDate: string
  monthlyRent: string
  currency: ContractCurrency
  adjustmentType: AdjustmentType
  adjustmentFrequency: AdjustmentFrequency
  adjustmentPercent: string
  lateFeeDailyRate: string
  lateFeeGraceDays: string
  dayOfMonth: string
  depositAmount: string
  notes: string
}

export const EMPTY_CONTRACT_FORM: ContractFormState = {
  propertyId: '', appTenantId: '',
  startDate: '', endDate: '',
  monthlyRent: '', currency: 'ARS',
  adjustmentType: 'ICL', adjustmentFrequency: 'Quarterly', adjustmentPercent: '',
  lateFeeDailyRate: '', lateFeeGraceDays: '0',
  dayOfMonth: '1', depositAmount: '', notes: '',
}

export const FREQ_OPTIONS: { value: AdjustmentFrequency; label: string }[] = [
  { value: 'Monthly', label: 'Mensual' },
  { value: 'Quarterly', label: 'Trimestral' },
  { value: 'FourMonthly', label: 'Cuatrimestral' },
  { value: 'SemiAnnual', label: 'Semestral' },
  { value: 'Annual', label: 'Anual' },
]

/** Carga el formulario con lo que ya tiene el contrato, para editarlo. */
export function contractFormFromDto(c: ContractDto): ContractFormState {
  return {
    propertyId: c.propertyId,
    appTenantId: c.appTenantId,
    // El DTO trae la fecha ISO completa; el input date sólo acepta yyyy-mm-dd.
    startDate: c.startDate.slice(0, 10),
    endDate: c.endDate.slice(0, 10),
    monthlyRent: String(c.monthlyRent),
    currency: c.currency,
    adjustmentType: c.adjustmentType,
    adjustmentFrequency: c.adjustmentFrequency,
    adjustmentPercent: c.adjustmentPercent != null ? String(c.adjustmentPercent) : '',
    lateFeeDailyRate: c.lateFeeDailyRate != null ? String(c.lateFeeDailyRate) : '',
    lateFeeGraceDays: String(c.lateFeeGraceDays),
    dayOfMonth: String(c.dayOfMonth),
    depositAmount: c.depositAmount != null ? String(c.depositAmount) : '',
    notes: c.notes ?? '',
  }
}

/** Traduce el formulario al cuerpo que espera la API. El PUT reemplaza el contrato entero. */
export function contractFormToRequest(form: ContractFormState): CreateContractRequest {
  return {
    propertyId: form.propertyId,
    appTenantId: form.appTenantId,
    startDate: form.startDate,
    endDate: form.endDate,
    monthlyRent: parseFloat(form.monthlyRent),
    currency: form.currency,
    adjustmentType: form.adjustmentType,
    adjustmentFrequency: form.adjustmentFrequency,
    // Sólo viaja en contratos de % fijo: el backend rechaza un porcentaje en los demás tipos.
    adjustmentPercent:
      form.adjustmentType === 'FixedPercent' && form.adjustmentPercent
        ? parseFloat(form.adjustmentPercent)
        : null,
    // Vacío significa "sin punitorio pactado", que no es lo mismo que 0: se manda null y el
    // backend no devenga nada.
    lateFeeDailyRate: form.lateFeeDailyRate ? parseFloat(form.lateFeeDailyRate) : null,
    lateFeeGraceDays: parseInt(form.lateFeeGraceDays || '0', 10),
    dayOfMonth: parseInt(form.dayOfMonth, 10),
    depositAmount: form.depositAmount ? parseFloat(form.depositAmount) : null,
    notes: form.notes.trim() || null,
  }
}
