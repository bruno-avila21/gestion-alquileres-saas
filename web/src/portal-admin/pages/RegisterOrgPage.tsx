import { Link } from 'react-router'
import { RegisterOrgForm } from '@/features/auth/components/RegisterOrgForm'
import { useRegisterOrg } from '@/features/auth/hooks/useRegisterOrg'

export default function RegisterOrgPage() {
  const mutation = useRegisterOrg()
  const errorMessage = mutation.isError ? 'No se pudo registrar (slug en uso o datos inválidos)' : undefined

  return (
    <div style={{ minHeight: '100vh', display: 'grid', placeItems: 'center', background: 'var(--surface-2)', padding: 24 }}>
      <div className="card" style={{ width: '100%', maxWidth: 420, padding: 24 }}>
        <h1 style={{ fontSize: 20, fontWeight: 600, letterSpacing: '-.01em', margin: '0 0 16px' }}>
          Registrar organización
        </h1>
        <RegisterOrgForm
          onSubmit={(r) => mutation.mutate(r)}
          isPending={mutation.isPending}
          errorMessage={errorMessage}
        />
        <div style={{ marginTop: 16, fontSize: 'var(--fs-sm)', color: 'var(--muted)' }}>
          ¿Ya tenés cuenta?{' '}
          <Link to="/admin/login" style={{ color: 'var(--brand-700)', fontWeight: 500 }}>Ingresar</Link>
        </div>
      </div>
    </div>
  )
}
