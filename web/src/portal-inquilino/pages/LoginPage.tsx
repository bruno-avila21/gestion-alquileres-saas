import { Link } from 'react-router'
import { LoginForm } from '@/features/auth/components/LoginForm'
import { useTenantLogin } from '@/features/auth/hooks/useTenantLogin'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card'

export default function TenantLoginPage() {
  const mutation = useTenantLogin()
  const errorMessage = mutation.isError ? 'Credenciales inválidas' : undefined
  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 p-6">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>Portal del Inquilino</CardTitle>
        </CardHeader>
        <CardContent>
          <LoginForm
            onSubmit={(r) => mutation.mutate(r)}
            isPending={mutation.isPending}
            errorMessage={errorMessage}
            submitLabel="Ingresar al portal"
          />
          <div className="mt-4 text-sm text-slate-600">
            ¿Administrador? <Link to="/admin/login" className="text-blue-600 underline">Portal admin</Link>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
