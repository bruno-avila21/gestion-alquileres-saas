import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { appTenantService } from '../services/appTenantService'
import type { CreateAppTenantRequest, UpdateAppTenantRequest } from '../types/apptenant.types'

const KEY = ['tenants']

export function useAppTenants() {
  return useQuery({ queryKey: KEY, queryFn: appTenantService.list })
}

export function useCreateAppTenant() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (req: CreateAppTenantRequest) => appTenantService.create(req),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}

export function useUpdateAppTenant() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, req }: { id: string; req: UpdateAppTenantRequest }) =>
      appTenantService.update(id, req),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}

export function useDeleteAppTenant() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => appTenantService.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}

export function useInviteTenant() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => appTenantService.invite(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}
