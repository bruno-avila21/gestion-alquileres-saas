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

export interface UpdateContractRequest extends CreateContractRequest {}

export interface TerminateContractRequest {
  notes?: string | null
}
