import { api } from '@/shared/lib/api'
import type { SiteSettingsDto } from '@/features/public/utils/siteTheme'

export const siteSettingsService = {
  async get(): Promise<SiteSettingsDto> {
    const { data } = await api.get<SiteSettingsDto>('/site-settings')
    return data
  },
  async update(req: SiteSettingsDto): Promise<SiteSettingsDto> {
    const { data } = await api.put<SiteSettingsDto>('/site-settings', req)
    return data
  },
}
