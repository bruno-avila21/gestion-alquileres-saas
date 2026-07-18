import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import type { RegisterOrgRequest } from '../types/auth.types'

// Mensajes en español en todas las reglas (audit A8): Zod emitía "String must contain at least 1
// character(s)" / "Invalid email" en inglés en el primer contacto con el producto. El mínimo de la
// contraseña sube a 12 para coincidir con la política del backend (audit B5).
const schema = z.object({
  organizationName: z.string().min(1, 'Ingresá el nombre de la organización').max(200, 'Máximo 200 caracteres'),
  slug: z.string().min(1, 'Ingresá un slug').max(100, 'Máximo 100 caracteres').regex(/^[a-z0-9-]+$/, 'Slug: solo minúsculas, números y guiones'),
  adminEmail: z.string().min(1, 'Ingresá el email').email('Email inválido').max(320, 'Máximo 320 caracteres'),
  adminPassword: z.string().min(12, 'Mínimo 12 caracteres').max(100, 'Máximo 100 caracteres'),
  adminFirstName: z.string().min(1, 'Ingresá el nombre').max(100, 'Máximo 100 caracteres'),
  adminLastName: z.string().min(1, 'Ingresá el apellido').max(100, 'Máximo 100 caracteres'),
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
