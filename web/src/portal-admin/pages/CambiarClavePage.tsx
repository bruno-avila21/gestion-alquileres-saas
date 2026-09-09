import { useNavigate } from 'react-router'
import { ChangePasswordForm } from '@/features/auth/components/ChangePasswordForm'
import { useAuthStore } from '@/shared/stores/authStore'

export default function AdminCambiarClavePage() {
  const navigate = useNavigate()
  const forced = useAuthStore((s) => s.user?.mustChangePassword ?? false)

  return (
    <div style={{ maxWidth: 460 }}>
      <h1 style={{ fontSize: 22, fontWeight: 600, letterSpacing: '-.01em', margin: '0 0 6px' }}>
        {forced ? 'Elegí tu contraseña' : 'Cambiar contraseña'}
      </h1>
      <p style={{ color: 'var(--muted)', fontSize: 'var(--fs-sm)', margin: '0 0 20px' }}>
        Se aplica a tu usuario, no al de la organización.
      </p>
      <ChangePasswordForm forced={forced} onDone={() => navigate('/admin', { replace: true })} />
    </div>
  )
}
