import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { siteSettingsService } from '../services/siteSettingsService'
import type { SiteSettingsDto } from '@/features/public/utils/siteTheme'

export const SITE_SETTINGS_KEY = ['site-settings'] as const

export function useSiteSettings() {
  return useQuery({ queryKey: SITE_SETTINGS_KEY, queryFn: siteSettingsService.get })
}

export function useUpdateSiteSettings() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (req: SiteSettingsDto) => siteSettingsService.update(req),
    onSuccess: (saved) => qc.setQueryData(SITE_SETTINGS_KEY, saved),
  })
}
