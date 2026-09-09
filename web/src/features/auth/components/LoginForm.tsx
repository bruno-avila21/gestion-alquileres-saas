import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import type { LoginRequest } from '../types/auth.types'
import { readLastOrgSlug } from '../utils/lastOrg'

const schema = z.object({
  organizationSlug: z.string()
    .min(1, 'Indicá la organización')
    .regex(/^[a-z0-9-]+$/, 'Va en minúsculas, sin espacios ni acentos (ej. palavecino)'),
  email: z.string().email('Email inválido'),
  password: z.string().min(1, 'Contraseña requerida'),
})

type FormValues = z.infer<typeof schema>

interface Props {
  onSubmit: (req: LoginRequest) => void
  isPending: boolean
  errorMessage?: string
  submitLabel: string
}

/**
 * Valor inicial del campo "Organización": el `?org=` del link que reparte la
 * inmobiliaria, o la última sesión de este navegador. Se lee de
 * `window.location` en vez de `useSearchParams` a propósito — es el valor de
 * arranque de un input no controlado, no hace falta suscribirse a la URL, y
 * así `LoginForm` no exige estar dentro de un Router para renderizar.
 */
function initialOrgSlug(): string {
  const fromUrl = typeof window === 'undefined'
    ? null
    : new URLSearchParams(window.location.search).get('org')
  return (fromUrl ?? readLastOrgSlug() ?? '').toLowerCase()
}

export function LoginForm({ onSubmit, isPending, errorMessage, submitLabel }: Props) {
  const defaultSlug = initialOrgSlug()

  const { register, handleSubmit, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { organizationSlug: defaultSlug, email: '', password: '' },
  })

  const errStyle = { fontSize: 'var(--fs-xs)', color: 'var(--danger)' } as const
  const hintStyle = { fontSize: 'var(--fs-xs)', color: 'var(--muted)' } as const

  return (
    <form
      onSubmit={handleSubmit((values) => onSubmit(values))}
      style={{ display: 'flex', flexDirection: 'column', gap: 16 }}
      aria-label="login-form"
    >
      <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
        <label className="label" htmlFor="organizationSlug">Organización</label>
        <input
          id="organizationSlug"
          className="input"
          placeholder="palavecino"
          autoCapitalize="none"
          autoCorrect="off"
          spellCheck={false}
          aria-describedby="organizationSlug-hint"
          {...register('organizationSlug')}
        />
        {errors.organizationSlug
          ? <span role="alert" style={errStyle}>{errors.organizationSlug.message}</span>
          : <span id="organizationSlug-hint" style={hintStyle}>El identificador corto de tu inmobiliaria, el mismo que va en la URL de tu sitio.</span>}
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
        <label className="label" htmlFor="email">Email</label>
        <input id="email" className="input" type="email" autoComplete="email" {...register('email')} />
        {errors.email && <span role="alert" style={errStyle}>{errors.email.message}</span>}
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
        <label className="label" htmlFor="password">Contraseña</label>
        <input id="password" className="input" type="password" autoComplete="current-password" {...register('password')} />
        {errors.password && <span role="alert" style={errStyle}>{errors.password.message}</span>}
      </div>
      {errorMessage && <div role="alert" style={{ fontSize: 'var(--fs-sm)', color: 'var(--danger)' }}>{errorMessage}</div>}
      <button type="submit" className="btn btn--primary" disabled={isPending} style={{ justifyContent: 'center' }}>
        {isPending ? 'Ingresando…' : submitLabel}
      </button>
    </form>
  )
}
