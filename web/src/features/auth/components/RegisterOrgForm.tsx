import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import type { RegisterOrgRequest } from '../types/auth.types'

const schema = z.object({
  organizationName: z.string().min(1).max(200),
  slug: z.string().min(1).max(100).regex(/^[a-z0-9-]+$/, 'Slug: solo minúsculas, números y guiones'),
  adminEmail: z.string().email(),
  adminPassword: z.string().min(8, 'Mínimo 8 caracteres').max(100),
  adminFirstName: z.string().min(1).max(100),
  adminLastName: z.string().min(1).max(100),
})

type FormValues = z.infer<typeof schema>

interface Props {
  onSubmit: (req: RegisterOrgRequest) => void
  isPending: boolean
  errorMessage?: string
}

export function RegisterOrgForm({ onSubmit, isPending, errorMessage }: Props) {
  const { register, handleSubmit, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { organizationName: '', slug: '', adminEmail: '', adminPassword: '', adminFirstName: '', adminLastName: '' },
  })

  const fields: Array<{ name: keyof FormValues; label: string; type?: string }> = [
    { name: 'organizationName', label: 'Nombre de organización' },
    { name: 'slug', label: 'Slug (URL)' },
    { name: 'adminFirstName', label: 'Nombre' },
    { name: 'adminLastName', label: 'Apellido' },
    { name: 'adminEmail', label: 'Email admin', type: 'email' },
    { name: 'adminPassword', label: 'Contraseña admin', type: 'password' },
  ]

  return (
    <form onSubmit={handleSubmit((v) => onSubmit(v))} className="flex flex-col gap-4" aria-label="register-org-form">
      {fields.map((f) => (
        <div key={f.name} className="flex flex-col gap-1.5">
          <Label htmlFor={f.name}>{f.label}</Label>
          <Input id={f.name} type={f.type ?? 'text'} {...register(f.name)} />
          {errors[f.name] && (
            <span role="alert" className="text-sm text-red-600">{errors[f.name]?.message as string}</span>
          )}
        </div>
      ))}
      {errorMessage && <div role="alert" className="text-sm text-red-600">{errorMessage}</div>}
      <Button type="submit" disabled={isPending}>
        {isPending ? 'Registrando…' : 'Registrar organización'}
      </Button>
    </form>
  )
}
