import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { organizationService } from '../services/organizationService'
import type { UpdateOrganizationRequest } from '../types/organization.types'

export const ORGANIZATION_KEY = ['organization'] as const
const LOGO_KEY = [...ORGANIZATION_KEY, 'logo'] as const

export function useOrganization() {
  return useQuery({ queryKey: ORGANIZATION_KEY, queryFn: organizationService.get })
}

export function useUpdateOrganization() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (req: UpdateOrganizationRequest) => organizationService.update(req),
    onSuccess: () => qc.invalidateQueries({ queryKey: ORGANIZATION_KEY }),
  })
}

export function useUploadLogo() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (file: File) => organizationService.uploadLogo(file),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ORGANIZATION_KEY })
      qc.invalidateQueries({ queryKey: LOGO_KEY })
    },
  })
}

export function useDeleteLogo() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: () => organizationService.deleteLogo(),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ORGANIZATION_KEY })
      qc.invalidateQueries({ queryKey: LOGO_KEY })
    },
  })
}

/** Object URL del logo, sólo se pide si `hasLogo` — evita un 404 innecesario en cada visita. */
export function useOrganizationLogoUrl(hasLogo: boolean) {
  return useQuery({
    queryKey: LOGO_KEY,
    queryFn: organizationService.logoUrl,
    enabled: hasLogo,
  })
}
