export type AdjustmentType = 'ICL' | 'IPC' | 'Manual'
export type AdjustmentFrequency = 'Monthly' | 'Quarterly' | 'Annual'
export type ContractStatus = 'Active' | 'Expired' | 'Terminated'
export type ContractCurrency = 'ARS' | 'USD'

export interface ContractDto {
  id: string
  organizationId: string
  propertyId: string
  propertyAddress: string
  propertyCity: string
  appTenantId: string
  appTenantFullName: string
  startDate: string
  endDate: string
  monthlyRent: number
  currency: ContractCurrency
  adjustmentType: AdjustmentType
  adjustmentFrequency: AdjustmentFrequency
  dayOfMonth: number
  depositAmount: number | null
  status: ContractStatus
  notes: string | null
  createdAt: string
  terminatedAt: string | null
}

export interface CreateContractRequest {
  propertyId: string
  appTenantId: string
  startDate: string
  endDate: string
  monthlyRent: number
  currency: ContractCurrency
  adjustmentType: AdjustmentType
  adjustmentFrequency: AdjustmentFrequency
  dayOfMonth: number
  depositAmount?: number | null
  notes?: string | null
}

export type UpdateContractRequest = CreateContractRequest

export interface TerminateContractRequest {
  notes?: string | null
}

export type TransactionType = 'RentCharge' | 'Payment' | 'ManualDebit' | 'ManualCredit'

export interface RentHistoryDto {
  id: string
  contractId: string
  previousRent: number
  newRent: number
  adjustmentFactor: number
  adjustmentType: AdjustmentType
  indexValueId: string | null
  effectiveDate: string
  notes: string | null
  createdAt: string
}

export interface TransactionDto {
  id: string
  contractId: string
  type: TransactionType
  amount: number
  currency: ContractCurrency
  period: string
  notes: string | null
  createdAt: string
}

export interface ApplyAdjustmentRequest {
  effectiveDate?: string | null
  manualNewRent?: number | null
  notes?: string | null
}

export interface RegisterPaymentRequest {
  amount: number
  period: string
  notes?: string | null
}

export interface RegisterManualTransactionRequest {
  type: 'ManualDebit' | 'ManualCredit'
  amount: number
  period: string
  notes: string
}
