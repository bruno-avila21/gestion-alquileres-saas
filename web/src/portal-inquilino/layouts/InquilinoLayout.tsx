import { Navigate, Outlet } from 'react-router'
import { useAuthStore } from '@/shared/stores/authStore'

export default function InquilinoLayout() {
  const user = useAuthStore((s) => s.user)
  if (!user || user.role !== 'Tenant') {
    return <Navigate to="/inquilino/login" replace />
  }
  return <Outlet />
}
