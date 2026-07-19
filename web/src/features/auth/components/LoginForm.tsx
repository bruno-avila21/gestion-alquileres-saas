import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import type { LoginRequest } from '../types/auth.types'

const schema = z.object({
  organizationSlug: z.string().min(1, 'Slug requerido').regex(/^[a-z0-9-]+$/, 'Slug inválido'),
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

export function LoginForm({ onSubmit, isPending, errorMessage, submitLabel }: Props) {
  const { register, handleSubmit, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { organizationSlug: '', email: '', password: '' },
  })

  const errStyle = { fontSize: 'var(--fs-xs)', color: 'var(--danger)' } as const

  return (
    <form
      onSubmit={handleSubmit((values) => onSubmit(values))}
      style={{ display: 'flex', flexDirection: 'column', gap: 16 }}
      aria-label="login-form"
    >
      <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
        <label className="label" htmlFor="organizationSlug">Organización</label>
        <input id="organizationSlug" className="input" placeholder="acme" {...register('organizationSlug')} />
        {errors.organizationSlug && <span role="alert" style={errStyle}>{errors.organizationSlug.message}</span>}
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
