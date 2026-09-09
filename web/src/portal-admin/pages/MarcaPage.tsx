import { useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { QueryError } from '@/shared/components/ui/QueryError'
import { IcCamera, IcUpload } from '@/shared/components/ui/Icons'
import {
  useDeleteLogo, useOrganization, useOrganizationLogoUrl, useUpdateOrganization, useUploadLogo,
} from '@/features/organization/hooks/useOrganization'
import type { UpdateOrganizationRequest } from '@/features/organization/types/organization.types'

const ACCEPTED_LOGO_TYPES = ['image/png', 'image/jpeg', 'image/webp']
const MAX_LOGO_BYTES = 2 * 1024 * 1024
const DEFAULT_BRAND_COLOR = '#4b5563'

// Mensajes en español, límites calcados de la validación del backend (Name 2–200, resto opcional
// con longitudes máximas, BrandColor #RRGGBB, Email con formato válido si viene).
const marcaSchema = z.object({
  name: z.string().min(2, 'Mínimo 2 caracteres').max(200, 'Máximo 200 caracteres'),
  legalName: z.string().max(200, 'Máximo 200 caracteres').optional().or(z.literal('')),
  taxId: z.string().max(20, 'Máximo 20 caracteres').optional().or(z.literal('')),
  address: z.string().max(300, 'Máximo 300 caracteres').optional().or(z.literal('')),
  phone: z.string().max(50, 'Máximo 50 caracteres').optional().or(z.literal('')),
  email: z.string().max(200, 'Máximo 200 caracteres').email('Email inválido').optional().or(z.literal('')),
  brandColor: z.string().regex(/^#[0-9A-Fa-f]{6}$/, 'Formato #RRGGBB').optional().or(z.literal('')),
})

type MarcaForm = z.infer<typeof marcaSchema>

const FIELDS: Array<{ name: keyof Omit<MarcaForm, 'brandColor'>; label: string; type?: string }> = [
  { name: 'name', label: 'Nombre comercial' },
  { name: 'legalName', label: 'Razón social' },
  { name: 'taxId', label: 'CUIT' },
  { name: 'address', label: 'Domicilio' },
  { name: 'phone', label: 'Teléfono' },
  { name: 'email', label: 'Email', type: 'email' },
]

export default function MarcaPage() {
  const { data: org, isLoading, isError, refetch } = useOrganization()
  const update = useUpdateOrganization()
  const uploadLogo = useUploadLogo()
  const deleteLogo = useDeleteLogo()
  const { data: logoUrl } = useOrganizationLogoUrl(org?.hasLogo ?? false)

  const fileRef = useRef<HTMLInputElement>(null)
  const [logoError, setLogoError] = useState('')
  const [saved, setSaved] = useState(false)

  const {
    register, handleSubmit, watch, setValue, formState: { errors },
  } = useForm<MarcaForm>({
    resolver: zodResolver(marcaSchema),
    values: org ? {
      name: org.name,
      legalName: org.legalName ?? '',
      taxId: org.taxId ?? '',
      address: org.address ?? '',
      phone: org.phone ?? '',
      email: org.email ?? '',
      brandColor: org.brandColor ?? '',
    } : undefined,
  })

  const brandColor = watch('brandColor')

  function onSubmit(values: MarcaForm) {
    setSaved(false)
    const req: UpdateOrganizationRequest = {
      name: values.name,
      legalName: values.legalName || null,
      taxId: values.taxId || null,
      address: values.address || null,
      phone: values.phone || null,
      email: values.email || null,
      brandColor: values.brandColor || null,
    }
    update.mutate(req, { onSuccess: () => setSaved(true) })
  }

  async function handleLogoFile(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    setLogoError('')
    if (!ACCEPTED_LOGO_TYPES.includes(file.type) || file.size > MAX_LOGO_BYTES) {
      setLogoError('El logo tiene que ser PNG, JPG o WEBP de hasta 2 MB.')
      if (fileRef.current) fileRef.current.value = ''
      return
    }
    try {
      await uploadLogo.mutateAsync(file)
    } catch {
      setLogoError('No pudimos subir el logo. Probá de nuevo.')
    }
    if (fileRef.current) fileRef.current.value = ''
  }

  return (
    <>
      <AdminTopbar crumbs={['Configuración', 'Marca']} />
      <div className="page">
        <div className="page-h">
          <div>
            <h1>Marca</h1>
            <div className="lead">Datos de la inmobiliaria que aparecen en recibos y liquidaciones</div>
          </div>
        </div>

        {isLoading ? (
          <div className="card" style={{ padding: 48, textAlign: 'center', color: 'var(--muted)' }}>Cargando…</div>
        ) : isError ? (
          <QueryError onRetry={() => refetch()} message="No pudimos cargar los datos de la organización." />
        ) : (
          <div style={{ maxWidth: 560, display: 'flex', flexDirection: 'column', gap: 'var(--s-7)' }}>
            <div className="card">
              <div className="card-h">
                <h3>Logo</h3>
                <div className="sub">encabezado de los PDF</div>
              </div>
              <div className="card-b" style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
                <div
                  style={{
                    width: 72, height: 72, borderRadius: 'var(--r-2)', border: '1px solid var(--hairline)',
                    display: 'flex', alignItems: 'center', justifyContent: 'center', overflow: 'hidden',
                    background: 'var(--surface-2)', flexShrink: 0,
                  }}
                >
                  {logoUrl ? (
                    <img src={logoUrl} alt="Logo de la inmobiliaria" style={{ width: '100%', height: '100%', objectFit: 'contain' }} />
                  ) : (
                    <IcCamera size={24} style={{ opacity: .3 }} />
                  )}
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                  <input
                    id="logo-input"
                    type="file"
                    accept={ACCEPTED_LOGO_TYPES.join(',')}
                    ref={fileRef}
                    style={{ display: 'none' }}
                    onChange={handleLogoFile}
                  />
                  <div className="row" style={{ gap: 8 }}>
                    <button
                      type="button"
                      className="btn btn--sm"
                      onClick={() => fileRef.current?.click()}
                      disabled={uploadLogo.isPending}
                    >
                      <IcUpload size={12} /> {uploadLogo.isPending ? 'Subiendo…' : org?.hasLogo ? 'Cambiar logo' : 'Subir logo'}
                    </button>
                    {org?.hasLogo && (
                      <button
                        type="button"
                        className="btn btn--sm btn--danger"
                        onClick={() => deleteLogo.mutate()}
                        disabled={deleteLogo.isPending}
                      >
                        Quitar
                      </button>
                    )}
                  </div>
                  <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>PNG, JPG o WEBP, hasta 2 MB.</div>
                  {logoError && (
                    <span role="alert" style={{ fontSize: 'var(--fs-xs)', color: 'var(--danger)' }}>{logoError}</span>
                  )}
                </div>
              </div>
            </div>

            <form onSubmit={handleSubmit(onSubmit)}>
              <div className="card">
                <div className="card-h">
                  <h3>Datos de la inmobiliaria</h3>
                </div>
                <div className="card-b" style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
                  {FIELDS.map((f) => (
                    <div key={f.name} style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                      <label className="label" htmlFor={f.name}>{f.label}</label>
                      <input id={f.name} className="input" type={f.type ?? 'text'} {...register(f.name)} />
                      {errors[f.name] && (
                        <span role="alert" style={{ fontSize: 'var(--fs-xs)', color: 'var(--danger)' }}>
                          {errors[f.name]?.message}
                        </span>
                      )}
                    </div>
                  ))}

                  <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                    <label className="label" htmlFor="brandColor">Color de marca</label>
                    <div className="row" style={{ gap: 8 }}>
                      <input
                        type="color"
                        aria-label="Selector de color de marca"
                        value={brandColor && /^#[0-9A-Fa-f]{6}$/.test(brandColor) ? brandColor : DEFAULT_BRAND_COLOR}
                        onChange={(e) => setValue('brandColor', e.target.value, { shouldValidate: true })}
                        style={{ width: 36, height: 32, padding: 0, border: '1px solid var(--hairline)', borderRadius: 'var(--r-2)' }}
                      />
                      <input
                        id="brandColor"
                        className="input"
                        style={{ flex: 1 }}
                        placeholder="#RRGGBB"
                        {...register('brandColor')}
                      />
                    </div>
                    {errors.brandColor && (
                      <span role="alert" style={{ fontSize: 'var(--fs-xs)', color: 'var(--danger)' }}>
                        {errors.brandColor.message}
                      </span>
                    )}
                  </div>

                  {update.isError && (
                    <div role="alert" style={{ fontSize: 'var(--fs-sm)', color: 'var(--danger)' }}>
                      No pudimos guardar los cambios. Revisá los datos e intentá de nuevo.
                    </div>
                  )}
                  {saved && !update.isPending && (
                    <div role="status" style={{ fontSize: 'var(--fs-sm)', color: 'var(--ok)' }}>
                      Cambios guardados.
                    </div>
                  )}

                  <button type="submit" className="btn btn--primary" disabled={update.isPending} style={{ alignSelf: 'flex-start' }}>
                    {update.isPending ? 'Guardando…' : 'Guardar cambios'}
                  </button>
                </div>
              </div>
            </form>
          </div>
        )}
      </div>
    </>
  )
}
