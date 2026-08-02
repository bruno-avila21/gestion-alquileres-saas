import { Navigate, Outlet, useLocation } from 'react-router'
import { useAuthStore } from '@/shared/stores/authStore'
import { TenantBottomNav } from '../components/TenantBottomNav'

const CHANGE_PASSWORD_PATH = '/inquilino/cambiar-clave'

export default function InquilinoLayout() {
  const user = useAuthStore((s) => s.user)
  const { pathname } = useLocation()

  if (!user || user.role !== 'Tenant') {
    return <Navigate to="/inquilino/login" replace />
  }

  // La contraseña que le pasó la inmobiliaria es temporal y quedó en esa conversación: no se
  // puede entrar al portal hasta elegir una propia.
  const mustChange = user.mustChangePassword && pathname !== CHANGE_PASSWORD_PATH
  if (mustChange) {
    return <Navigate to={CHANGE_PASSWORD_PATH} replace />
  }

  return (
    <div className="app t-portal" style={{ minHeight: '100vh', background: 'var(--surface-2)' }}>
      <Outlet />
      {/* Sin navegación mientras el cambio es obligatorio: no hay nada más que hacer todavía. */}
      {!user.mustChangePassword && <TenantBottomNav />}
    </div>
  )
}
