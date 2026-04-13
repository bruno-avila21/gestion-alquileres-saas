import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
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

  return (
    <form
      onSubmit={handleSubmit((values) => onSubmit(values))}
      className="flex flex-col gap-4"
      aria-label="login-form"
    >
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="organizationSlug">Organización</Label>
        <Input id="organizationSlug" placeholder="acme" {...register('organizationSlug')} />
        {errors.organizationSlug && (
          <span role="alert" className="text-sm text-red-600">{errors.organizationSlug.message}</span>
        )}
      </div>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="email">Email</Label>
        <Input id="email" type="email" autoComplete="email" {...register('email')} />
        {errors.email && <span role="alert" className="text-sm text-red-600">{errors.email.message}</span>}
      </div>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="password">Contraseña</Label>
        <Input id="password" type="password" autoComplete="current-password" {...register('password')} />
        {errors.password && <span role="alert" className="text-sm text-red-600">{errors.password.message}</span>}
      </div>
      {errorMessage && <div role="alert" className="text-sm text-red-600">{errorMessage}</div>}
      <Button type="submit" disabled={isPending}>
        {isPending ? 'Ingresando…' : submitLabel}
      </Button>
    </form>
  )
}
