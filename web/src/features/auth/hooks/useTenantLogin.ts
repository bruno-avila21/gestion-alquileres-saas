import { useMutation } from '@tanstack/react-query'
import { useNavigate } from 'react-router'
import { authService } from '../services/authService'
import { useAuthStore } from '@/shared/stores/authStore'
import type { LoginRequest } from '../types/auth.types'

export function useTenantLogin() {
  const login = useAuthStore((s) => s.login)
  const navigate = useNavigate()
  return useMutation({
    mutationFn: (req: LoginRequest) => authService.tenantLogin(req),
    onSuccess: (data) => {
      login(data.token, {
        userId: data.userId,
        email: data.email,
        role: data.role,
        organizationId: data.organizationId,
        organizationSlug: data.organizationSlug,
      })
      navigate('/inquilino', { replace: true })
    },
  })
}
