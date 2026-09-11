export interface LoginRequest {
  email: string
  password: string
  organizationSlug: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export interface RegisterOrgRequest {
  organizationName: string
  slug: string
  adminEmail: string
  adminPassword: string
  adminFirstName: string
  adminLastName: string
}

export interface AuthResponse {
  token: string
  userId: string
  email: string
  role: 'Admin' | 'Staff' | 'Tenant'
  organizationId: string
  organizationSlug: string
  mustChangePassword: boolean
}
