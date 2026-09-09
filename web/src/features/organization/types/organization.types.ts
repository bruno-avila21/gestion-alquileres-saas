export interface OrganizationDto {
  id: string
  name: string
  legalName: string | null
  taxId: string | null
  address: string | null
  phone: string | null
  email: string | null
  brandColor: string | null
  hasLogo: boolean
  plan: string
}

export interface UpdateOrganizationRequest {
  name: string
  legalName: string | null
  taxId: string | null
  address: string | null
  phone: string | null
  email: string | null
  brandColor: string | null
}
