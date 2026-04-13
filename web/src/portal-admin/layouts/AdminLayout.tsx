import { Navigate, Outlet } from 'react-router'
import { useAuthStore } from '@/shared/stores/authStore'

export default function AdminLayout() {
  const user = useAuthStore((s) => s.user)
  if (!user || user.role === 'Tenant') {
    return <Navigate to="/admin/login" replace />
  }
  return <Outlet />
}
