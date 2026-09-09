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
  periodFrom: string
  periodTo: string
  grossCollected: number
  commissionAmount: number
  netToOwner: number
  lines: OwnerSettlementLineDto[]
}
