import { api } from '@/shared/lib/api'
import type { DashboardDto } from '../types/dashboard.types'

export const dashboardService = {
  async get(): Promise<DashboardDto> {
    const { data } = await api.get<DashboardDto>('/dashboard')
    return data
  },
}
