import { useAuthStore } from '@/shared/stores/authStore'
import { Button } from '@/shared/components/ui/button'

export default function DashboardPage() {
  const user = useAuthStore((s) => s.user)
  const logout = useAuthStore((s) => s.logout)
  return (
    <div className="p-6">
      <h1 className="text-2xl font-semibold">Panel administrativo</h1>
      <p className="mt-2 text-slate-600">
        Bienvenido {user?.email} (rol: {user?.role}, org: {user?.organizationSlug})
      </p>
      <Button className="mt-4" variant="outline" onClick={logout}>Cerrar sesión</Button>
    </div>
  )
}
