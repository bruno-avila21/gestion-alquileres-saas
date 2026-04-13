import { api } from '@/shared/lib/api'
import type { AuthResponse, LoginRequest, RegisterOrgRequest } from '../types/auth.types'

export const authService = {
  async login(req: LoginRequest): Promise<AuthResponse> {
    const { data } = await api.post<AuthResponse>('/auth/login', req)
    return data
  },
  async tenantLogin(req: LoginRequest): Promise<AuthResponse> {
    const { data } = await api.post<AuthResponse>('/auth/tenant-login', req)
    return data
  },
  async registerOrg(req: RegisterOrgRequest): Promise<AuthResponse> {
    const { data } = await api.post<AuthResponse>('/auth/register-org', req)
    return data
  },
}
