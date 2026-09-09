export interface OwnerDto {
  id: string
  organizationId: string
  name: string
  taxId: string | null
  email: string | null
  phone: string | null
  cbu: string | null
  notes: string | null
  isActive: boolean
  createdAt: string
}

export interface OwnerSettlementLineDto {
  propertyId: string
  propertyAddress: string
  contractId: string
  collected: number
  commissionPct: number
  commission: number
  net: number
}

export interface OwnerSettlementDto {
  ownerId: string
  ownerName: string
  /** CBU o alias. Null = no lo cargaron todavía; sin esto no se le puede transferir. */
  ownerCbu: string | null
  periodFrom: string
  periodTo: string
  grossCollected: number
  commissionAmount: number
  netToOwner: number
  lines: OwnerSettlementLineDto[]
}

/** Un período de una simulación de ajuste. Espeja `AdjustmentProjectionItem` del dominio. */
export interface AdjustmentProjectionItem {
  number: number
  from: string
  to: string
  rent: number | null
  coefficient: number | null
  variationPct: number | null
  indexAvailable: boolean
}

export interface AdjustmentProjection {
  currentRent: number
  schedule: AdjustmentProjectionItem[]
  notes: string | null
}

export interface SimulateAdjustmentParams {
  type: 'ICL' | 'IPC' | 'FixedPercent'
  initialRent: number
  startDate: string
  frequency: 'Monthly' | 'Quarterly' | 'FourMonthly' | 'SemiAnnual' | 'Annual'
  percent?: number
}
