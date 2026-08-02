import { Navigate, Outlet, useLocation } from 'react-router'
import { useAuthStore } from '@/shared/stores/authStore'
import { AdminSidebar } from './AdminSidebar'

const CHANGE_PASSWORD_PATH = '/admin/cambiar-clave'

export default function AdminLayout() {
  const user = useAuthStore((s) => s.user)
  const { pathname } = useLocation()

  if (!user || user.role === 'Tenant') {
    return <Navigate to="/admin/login" replace />
  }

  // Credencial generada por el sistema: no se puede operar hasta cambiarla.
  if (user.mustChangePassword && pathname !== CHANGE_PASSWORD_PATH) {
    return <Navigate to={CHANGE_PASSWORD_PATH} replace />
  }
  return (
    <div className="app shell" style={{ height: '100vh' }}>
      <a href="#main-content" className="skip-link">Saltar al contenido</a>
      <AdminSidebar />
      <div className="main" id="main-content">
        <Outlet />
      </div>
    </div>
  )
}
