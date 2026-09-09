import type { ListingOperationType } from '@/features/listings/types/listing.types'

export type LeadSource = 'Website' | 'Manual'

/** Orden fijo del Kanban — el mismo orden que declara el enum en la API. */
export type LeadStatus = 'New' | 'Contacted' | 'Visit' | 'Negotiation' | 'Won' | 'Lost'

export const LEAD_STATUSES: LeadStatus[] = ['New', 'Contacted', 'Visit', 'Negotiation', 'Won', 'Lost']

export const LEAD_STATUS_LABELS: Record<LeadStatus, string> = {
  New: 'Nueva',
  Contacted: 'Contactada',
  Visit: 'Visita',
  Negotiation: 'Negociación',
  Won: 'Ganada',
  Lost: 'Perdida',
}

export interface LeadDto {
  id: string
  name: string
  email: string | null
  phone: string | null
  message: string
  source: LeadSource
  status: LeadStatus
  lostReason: string | null
  listingId: string | null
  propertyId: string | null
  propertyTitle: string | null
  propertyAddress: string | null
  listingOperation: ListingOperationType | null
  createdAt: string
  updatedAt: string
  lastContactAt: string | null
  notesCount: number
}

export interface LeadNoteDto {
  id: string
  text: string
  createdByName: string
  createdAt: string
}

export interface LeadDetailDto extends LeadDto {
  /** Ordenadas desc por createdAt (la más nueva primero). */
  notes: LeadNoteDto[]
}

/** Claves = nombre del enum `LeadStatus`. La API puede omitir estados sin consultas todavía. */
export type LeadStatusCounts = Partial<Record<LeadStatus, number>>

export interface LeadSummaryDto {
  total: number
  byStatus: LeadStatusCounts
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface LeadFilters {
  status?: LeadStatus
  search?: string
  page?: number
  pageSize?: number
}

export interface CreateLeadRequest {
  name: string
  email?: string
  phone?: string
  message: string
  listingId?: string
}

export interface UpdateLeadRequest {
  name: string
  email?: string
  phone?: string
  message: string
}

export interface UpdateLeadStatusRequest {
  status: LeadStatus
  /** Obligatorio cuando `status` es `Lost`. */
  lostReason?: string
}

export interface AddLeadNoteRequest {
  text: string
}
