import { Link } from 'react-router'
import { useTenantLogin } from '@/features/auth/hooks/useTenantLogin'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import type { LoginRequest } from '@/features/auth/types/auth.types'
import { IcArrowR } from '@/shared/components/ui/Icons'

const schema = z.object({
  email: z.string().email('Email inválido'),
  password: z.string().min(1, 'Contraseña requerida'),
})

type FormValues = z.infer<typeof schema>

export default function TenantLoginPage() {
  const mutation = useTenantLogin()

  const { register, handleSubmit, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: '', password: '' },
  })

  function onSubmit(values: FormValues) {
    const req: LoginRequest = { ...values, organizationSlug: '' }
    mutation.mutate(req)
  }

  return (
    <div
      className="app t-portal"
      style={{
        minHeight: '100vh',
        display: 'grid',
        gridTemplateColumns: '1fr 1.1fr',
        background: 'var(--surface)',
      }}
    >
      {/* Columna izquierda: formulario */}
      <div style={{ padding: '64px 88px', display: 'flex', flexDirection: 'column', gap: 40 }}>
        {/* Logo */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <div
            style={{
              width: 32, height: 32, borderRadius: 9,
              background: 'var(--brand)', color: 'var(--on-brand)',
              display: 'grid', placeItems: 'center',
              fontWeight: 700, fontSize: 16, letterSpacing: '-.04em',
            }}
          >
            A
          </div>
          <div style={{ fontWeight: 600, fontSize: 16, letterSpacing: '-.01em' }}>Alquilar.io</div>
          <div style={{ marginLeft: 'auto', fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>
            Portal Inquilino
          </div>
        </div>

        {/* Formulario */}
        <div
          style={{
            flex: 1, display: 'flex', flexDirection: 'column',
            justifyContent: 'center', gap: 24, maxWidth: 380,
          }}
        >
          <div>
            <h1 style={{ margin: 0, fontSize: 30, fontWeight: 600, letterSpacing: '-.02em' }}>Ingresar</h1>
            <div style={{ color: 'var(--muted)', marginTop: 8, fontSize: 'var(--fs-sm)' }}>
              Consultá tu contrato, pagos y próximos ajustes.
            </div>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
            <div className="col" style={{ gap: 6 }}>
              <span className="label">Email</span>
              <input
                className="input"
                type="email"
                placeholder="tu@email.com"
                autoComplete="email"
                {...register('email')}
              />
              {errors.email && (
                <span role="alert" style={{ fontSize: 'var(--fs-xs)', color: 'var(--danger)' }}>
                  {errors.email.message}
                </span>
              )}
            </div>

            <div className="col" style={{ gap: 6 }}>
              <div className="between">
                <span className="label">Contraseña</span>
                <a style={{ fontSize: 'var(--fs-xs)', color: 'var(--brand)', cursor: 'pointer', textDecoration: 'none' }}>
                  Olvidé mi contraseña
                </a>
              </div>
              <input
                className="input"
                type="password"
                autoComplete="current-password"
                {...register('password')}
              />
              {errors.password && (
                <span role="alert" style={{ fontSize: 'var(--fs-xs)', color: 'var(--danger)' }}>
                  {errors.password.message}
                </span>
              )}
            </div>

            {mutation.isError && (
              <div role="alert" style={{ fontSize: 'var(--fs-xs)', color: 'var(--danger)' }}>
                Credenciales inválidas
              </div>
            )}

            <button
              type="submit"
              disabled={mutation.isPending}
              className="btn btn--primary"
              style={{ height: 44, justifyContent: 'center', fontSize: 'var(--fs-body)' }}
            >
              {mutation.isPending ? 'Ingresando…' : 'Ingresar'}
              {!mutation.isPending && <IcArrowR size={14} />}
            </button>
          </form>

          <Link
            to="/admin/login"
            style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', textDecoration: 'none' }}
          >
            ¿Administrador? Portal admin
          </Link>
        </div>

        <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>
          © 2026 Alquilar.io · Datos protegidos por Ley 25.326
        </div>
      </div>

      {/* Columna derecha: panel decorativo */}
      <div
        style={{
          background: 'linear-gradient(140deg, var(--brand-50), var(--surface-2))',
          padding: 56,
          position: 'relative',
          overflow: 'hidden',
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'flex-end',
        }}
      >
        {/* Patrón de dots */}
        <svg
          style={{ position: 'absolute', top: 40, right: 40, opacity: 0.14 }}
          width="320"
          height="320"
          viewBox="0 0 320 320"
        >
          <defs>
            <pattern id="dots-tenant" width="14" height="14" patternUnits="userSpaceOnUse">
              <circle cx="2" cy="2" r="1" fill="var(--brand)" />
            </pattern>
          </defs>
          <rect width="320" height="320" fill="url(#dots-tenant)" />
        </svg>

        {/* Card de próximo pago */}
        <div style={{ position: 'absolute', top: 56, left: 56, right: 56 }}>
          <div className="card" style={{ padding: 18, boxShadow: 'var(--sh-3)', maxWidth: 360 }}>
            <div className="between" style={{ marginBottom: 10 }}>
              <span className="chip chip--ok"><span className="dot" />Al día</span>
              <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>Mayo 2026</span>
            </div>
            <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', textTransform: 'uppercase', letterSpacing: '.04em' }}>
              Próximo pago
            </div>
            <div style={{ fontSize: 28, fontWeight: 600, letterSpacing: '-.02em', fontVariantNumeric: 'tabular-nums', marginTop: 4 }}>
              $ 485.000
            </div>
            <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', marginTop: 4 }}>vence 10 mayo</div>
          </div>

          <div className="card" style={{ padding: 18, marginTop: 14, marginLeft: 60, boxShadow: 'var(--sh-3)', maxWidth: 320 }}>
            <div className="between" style={{ marginBottom: 8 }}>
              <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', textTransform: 'uppercase', letterSpacing: '.04em' }}>
                Próximo ajuste
              </span>
              <span className="chip chip--icl" style={{ height: 18, fontSize: 10 }}><span className="dot" />ICL</span>
            </div>
            <div style={{ fontSize: 20, fontWeight: 600, color: 'var(--brand-700)', fontVariantNumeric: 'tabular-nums' }}>
              15 jul · +12,4%
            </div>
            <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', marginTop: 2 }}>$ 485.000 → $ 545.000 (est.)</div>
          </div>
        </div>

        {/* Texto inferior */}
        <div style={{ position: 'relative', maxWidth: 480 }}>
          <div style={{ fontSize: 32, fontWeight: 600, letterSpacing: '-.02em', lineHeight: 1.15 }}>
            Tu alquiler, claro y sin sorpresas.
          </div>
          <div style={{ marginTop: 12, color: 'var(--ink-soft)', fontSize: 'var(--fs-body)', lineHeight: 1.55 }}>
            Conocé tu próximo ajuste antes de que pase. Descargá recibos y contratos cuando los necesites.
          </div>
        </div>
      </div>
    </div>
  )
}
