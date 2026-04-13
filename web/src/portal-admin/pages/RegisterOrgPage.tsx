import { Link } from 'react-router'
import { RegisterOrgForm } from '@/features/auth/components/RegisterOrgForm'
import { useRegisterOrg } from '@/features/auth/hooks/useRegisterOrg'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card'

export default function RegisterOrgPage() {
  const mutation = useRegisterOrg()
  const errorMessage = mutation.isError ? 'No se pudo registrar (slug en uso o datos inválidos)' : undefined

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 p-6">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>Registrar Organización</CardTitle>
        </CardHeader>
        <CardContent>
          <RegisterOrgForm
            onSubmit={(r) => mutation.mutate(r)}
            isPending={mutation.isPending}
            errorMessage={errorMessage}
          />
          <div className="mt-4 text-sm text-slate-600">
            ¿Ya tienes cuenta? <Link to="/admin/login" className="text-blue-600 underline">Ingresar</Link>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
