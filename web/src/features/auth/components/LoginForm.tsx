import { useEffect, useState } from 'react'
import { isAxiosError } from 'axios'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { publicApi } from '@/shared/lib/publicApi'
import type { LoginRequest } from '../types/auth.types'
import { readLastOrgSlug } from '../utils/lastOrg'
import { CREDENTIAL_MISMATCH } from '../utils/loginError'

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

  // Ver la contraseña no es una comodidad: el campo Organización viene precargado, así que
  // el formulario se ve completo y el navegador puede autocompletar email y clave de otra
  // entrada guardada sin que se note. Poder mirar qué se está mandando cierra ese agujero.
  const [showPassword, setShowPassword] = useState(false)
  const [attemptedSlug, setAttemptedSlug] = useState<string | null>(null)
  const [orgExists, setOrgExists] = useState<boolean | null>(null)

  const credentialMismatch = errorMessage === CREDENTIAL_MISMATCH

  useEffect(() => {
    if (!credentialMismatch || !attemptedSlug) return

    let cancelled = false
    publicApi.get(`/public/${encodeURIComponent(attemptedSlug)}`)
      .then(() => { if (!cancelled) setOrgExists(true) })
      .catch((err: unknown) => {
        // Sólo un 404 prueba que no existe. Una caída de red o un 500 no dicen nada, y
        // afirmar "esa organización no existe" ahí mandaría a cambiar el campo correcto.
        if (!cancelled && isAxiosError(err) && err.response?.status === 404) setOrgExists(false)
      })
    return () => { cancelled = true }
  }, [credentialMismatch, attemptedSlug])

  const errStyle = { fontSize: 'var(--fs-xs)', color: 'var(--danger)' } as const
  const hintStyle = { fontSize: 'var(--fs-xs)', color: 'var(--muted)' } as const

  return (
    <form
      onSubmit={handleSubmit((values) => {
        setAttemptedSlug(values.organizationSlug)
        setOrgExists(null)
        onSubmit(values)
      })}
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
        <div style={{ position: 'relative', display: 'flex', alignItems: 'center' }}>
          <input
            id="password"
            className="input"
            type={showPassword ? 'text' : 'password'}
            autoComplete="current-password"
            style={{ width: '100%', paddingRight: 84 }}
            {...register('password')}
          />
          <button
            type="button"
            onClick={() => setShowPassword((v) => !v)}
            aria-pressed={showPassword}
            style={{
              position: 'absolute', right: 8, border: 'none', background: 'none',
              color: 'var(--muted)', fontSize: 'var(--fs-xs)', fontWeight: 600,
              cursor: 'pointer', padding: '4px 6px',
            }}
          >
            {showPassword ? 'Ocultar' : 'Mostrar'}
          </button>
        </div>
        {errors.password && <span role="alert" style={errStyle}>{errors.password.message}</span>}
      </div>
      {errorMessage && (
        <div role="alert" style={{ fontSize: 'var(--fs-sm)', color: 'var(--danger)' }}>
          {errorMessage}
          {credentialMismatch && orgExists === false ? (
            <div style={{ ...hintStyle, marginTop: 6 }}>
              La organización «{attemptedSlug}» no existe. Va el identificador corto, el mismo
              que aparece en la URL de tu sitio (…/sitio/<b>palavecino</b>), no el nombre de la
              inmobiliaria.
            </div>
          ) : null}
          {credentialMismatch && orgExists === true ? (
            <div style={{ ...hintStyle, marginTop: 6 }}>
              La organización «{attemptedSlug}» existe, así que lo que no coincide es el email o
              la contraseña. Tocá «Mostrar» para ver qué contraseña se está enviando: si el
              navegador autocompletó una guardada, va a estar ahí.
            </div>
          ) : null}
        </div>
      )}
      <button type="submit" className="btn btn--primary" disabled={isPending} style={{ justifyContent: 'center' }}>
        {isPending ? 'Ingresando…' : submitLabel}
      </button>
    </form>
  )
}
