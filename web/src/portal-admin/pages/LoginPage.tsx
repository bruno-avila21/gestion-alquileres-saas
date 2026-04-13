import { Link } from 'react-router'
import { LoginForm } from '@/features/auth/components/LoginForm'
import { useLogin } from '@/features/auth/hooks/useLogin'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card'

export default function AdminLoginPage() {
  const mutation = useLogin()
  const errorMessage = mutation.isError ? 'Credenciales inválidas' : undefined

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 p-6">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>Ingreso Administrador</CardTitle>
        </CardHeader>
        <CardContent>
          <LoginForm
            onSubmit={(r) => mutation.mutate(r)}
            isPending={mutation.isPending}
            errorMessage={errorMessage}
            submitLabel="Ingresar"
          />
          <div className="mt-4 text-sm text-slate-600">
            ¿Nueva inmobiliaria? <Link to="/admin/register-org" className="text-blue-600 underline">Registrarse</Link>
          </div>
          <div className="mt-2 text-sm text-slate-600">
            ¿Inquilino? <Link to="/inquilino/login" className="text-blue-600 underline">Portal de inquilinos</Link>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
