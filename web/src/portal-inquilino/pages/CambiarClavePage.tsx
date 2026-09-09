import { useNavigate } from 'react-router'
import { ChangePasswordForm } from '@/features/auth/components/ChangePasswordForm'
import { useAuthStore } from '@/shared/stores/authStore'

export default function TenantCambiarClavePage() {
  const navigate = useNavigate()
  const forced = useAuthStore((s) => s.user?.mustChangePassword ?? false)

  return (
    <div style={{ maxWidth: 420, margin: '0 auto', padding: '28px 18px 100px' }}>
      <h1 style={{ fontSize: 20, fontWeight: 600, letterSpacing: '-.01em', margin: '0 0 18px' }}>
        {forced ? 'Elegí tu contraseña' : 'Cambiar contraseña'}
      </h1>
      <ChangePasswordForm forced={forced} onDone={() => navigate('/inquilino', { replace: true })} />
    </div>
  )
}
