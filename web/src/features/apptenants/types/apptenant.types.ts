export interface AppTenantDto {
  id: string
  organizationId: string
  firstName: string
  lastName: string
  dni: string
  email: string | null
  phone: string | null
  userId: string | null
  isActive: boolean
  createdAt: string
}

export interface CreateAppTenantRequest {
  firstName: string
  lastName: string
  dni: string
  email?: string | null
  phone?: string | null
}

export interface UpdateAppTenantRequest extends CreateAppTenantRequest {
  isActive: boolean
}

export interface InviteTenantResult {
  tenant: AppTenantDto
  tempPassword: string
}
