import { Link } from 'react-router'
import { LoginForm } from '@/features/auth/components/LoginForm'
import { useLogin } from '@/features/auth/hooks/useLogin'
import { IcArrowR } from '@/shared/components/ui/Icons'

const ICL_SERIES = [1.1218, 1.1542, 1.1981, 1.2358, 1.2794, 1.3210, 1.3680, 1.4115, 1.4602, 1.5104, 1.5621, 1.6210]
const IPC_SERIES = [3.8, 4.2, 4.6, 4.1, 3.7, 3.4, 3.1, 2.9, 2.7, 2.6, 2.5, 2.4]

function MiniSpark({ data, color, w = 280, h = 48 }: { data: number[]; color: string; w?: number; h?: number }) {
  const max = Math.max(...data)
  const min = Math.min(...data)
  const span = max - min || 1
  const pts = data.map((v, i) => {
    const x = (i / (data.length - 1)) * w
    const y = h - ((v - min) / span) * h
    return `${x.toFixed(1)},${y.toFixed(1)}`
  }).join(' ')
  const areaPts = `0,${h} ${pts} ${w},${h}`
  return (
    <svg width="100%" height={h} viewBox={`0 0 ${w} ${h}`} style={{ display: 'block' }} preserveAspectRatio="none">
      <polygon points={areaPts} fill={color} opacity={0.1} />
      <polyline points={pts} fill="none" stroke={color} strokeWidth="1.5" strokeLinejoin="round" strokeLinecap="round" />
    </svg>
  )
}

export default function AdminLoginPage() {
  const mutation = useLogin()
  const errorMessage = mutation.isError ? 'Credenciales inválidas' : undefined

  return (
    <div
      className="app"
      style={{
        minHeight: '100vh',
        display: 'grid',
        gridTemplateColumns: '1fr 1.1fr',
        background: 'var(--surface)',
      }}
    >
      {/* Columna izquierda: formulario */}
      <div style={{ padding: '64px 88px', display: 'flex', flexDirection: 'column', gap: 40 }}>
        {/* Logo + título */}
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
            Portal Administrador
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
              Administrá contratos, ajustes ICL/IPC y propiedades.
            </div>
          </div>

          <LoginForm
            onSubmit={(r) => mutation.mutate(r)}
            isPending={mutation.isPending}
            errorMessage={errorMessage}
            submitLabel="Ingresar"
          />

          <div style={{ display: 'flex', alignItems: 'center', gap: 12, color: 'var(--muted)', fontSize: 'var(--fs-xs)' }}>
            <div style={{ flex: 1, height: 1, background: 'var(--hairline)' }} />
            <span>O</span>
            <div style={{ flex: 1, height: 1, background: 'var(--hairline)' }} />
          </div>

          <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
            <Link
              to="/admin/register-org"
              style={{ fontSize: 'var(--fs-xs)', color: 'var(--brand)', textDecoration: 'none' }}
            >
              ¿Nueva inmobiliaria? Registrarse <IcArrowR size={12} />
            </Link>
            <Link
              to="/inquilino/login"
              style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', textDecoration: 'none' }}
            >
              ¿Inquilino? Portal de inquilinos
            </Link>
          </div>
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
        {/* Patrón de dots decorativo */}
        <svg
          style={{ position: 'absolute', top: 40, right: 40, opacity: 0.14 }}
          width="320"
          height="320"
          viewBox="0 0 320 320"
        >
          <defs>
            <pattern id="dots" width="14" height="14" patternUnits="userSpaceOnUse">
              <circle cx="2" cy="2" r="1" fill="var(--brand)" />
            </pattern>
          </defs>
          <rect width="320" height="320" fill="url(#dots)" />
        </svg>

        {/* Cards de índices flotantes */}
        <div style={{ position: 'absolute', top: 56, left: 56, right: 56 }}>
          <div className="card" style={{ padding: 18, boxShadow: 'var(--sh-3)', maxWidth: 420 }}>
            <div className="between" style={{ marginBottom: 10 }}>
              <span className="chip chip--icl"><span className="dot" />ICL</span>
              <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>BCRA · hoy</span>
            </div>
            <div
              style={{
                fontSize: 24, fontWeight: 600, letterSpacing: '-.02em',
                fontVariantNumeric: 'tabular-nums',
              }}
            >
              1,6210
            </div>
            <div className="delta-pill up" style={{ marginTop: 4, display: 'inline-flex', gap: 4, alignItems: 'center' }}>
              +3,87% trim.
            </div>
            <MiniSpark data={ICL_SERIES} color="var(--icl)" />
          </div>

          <div
            className="card"
            style={{ padding: 18, marginTop: 14, marginLeft: 80, boxShadow: 'var(--sh-3)', maxWidth: 360 }}
          >
            <div className="between" style={{ marginBottom: 10 }}>
              <span className="chip chip--ipc"><span className="dot" />IPC</span>
              <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>INDEC · abr 2026</span>
            </div>
            <div
              style={{
                fontSize: 24, fontWeight: 600, letterSpacing: '-.02em',
                fontVariantNumeric: 'tabular-nums',
              }}
            >
              2,4%
            </div>
            <div
              className="delta-pill"
              style={{ marginTop: 4, display: 'inline-flex', gap: 4, alignItems: 'center', background: 'var(--ok-50)', color: 'var(--ok)' }}
            >
              −0,1 pp
            </div>
            <MiniSpark data={IPC_SERIES} color="var(--ipc)" />
          </div>
        </div>

        {/* Texto inferior */}
        <div style={{ position: 'relative', maxWidth: 480 }}>
          <div
            style={{
              fontSize: 32, fontWeight: 600, letterSpacing: '-.02em', lineHeight: 1.15,
            }}
          >
            Ajustes ICL e IPC, automatizados y trazables.
          </div>
          <div
            style={{
              marginTop: 12, color: 'var(--ink-soft)',
              fontSize: 'var(--fs-body)', lineHeight: 1.55,
            }}
          >
            Conectado al BCRA y al INDEC. Tus contratos se ajustan a tiempo y con
            un email automático para cada inquilino.
          </div>
        </div>
      </div>
    </div>
  )
}
