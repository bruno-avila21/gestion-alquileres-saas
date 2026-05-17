import { Navigate, Outlet } from 'react-router'
import { useAuthStore } from '@/shared/stores/authStore'
import { AdminSidebar } from './AdminSidebar'

export default function AdminLayout() {
  const user = useAuthStore((s) => s.user)
  if (!user || user.role === 'Tenant') {
    return <Navigate to="/admin/login" replace />
  }
  return (
    <div className="app shell" style={{ height: '100vh' }}>
      <AdminSidebar />
      <div className="main">
        <Outlet />
      </div>
    </div>
  )
}
