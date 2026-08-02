import { useMutation } from '@tanstack/react-query'
import { authService } from '../services/authService'
import { useAuthStore } from '@/shared/stores/authStore'
import type { ChangePasswordRequest } from '../types/auth.types'

export function useChangePassword() {
  const login = useAuthStore((s) => s.login)
  return useMutation({
    mutationFn: (req: ChangePasswordRequest) => authService.changePassword(req),
    onSuccess: (data) => {
      // El backend emite un par de tokens nuevo para esta sesión, así que se refresca el perfil
      // en vez de expulsar al usuario: baja mustChangePassword y sigue trabajando.
      login({
        userId: data.userId,
        email: data.email,
        role: data.role,
        organizationId: data.organizationId,
        organizationSlug: data.organizationSlug,
        mustChangePassword: data.mustChangePassword,
      })
    },
  })
}
