import { useState } from 'react'
import { useCreatePublicLead } from '../hooks/usePublic'
import { CheckIcon } from '@/portal-publico/components/icons'

interface LeadFormProps {
  slug: string
  /** Presente cuando el formulario se dispara desde la ficha de una propiedad puntual. */
  listingId?: string
  title?: string
  description?: string
}

type Status = 'idle' | 'sending' | 'sent' | 'error'

interface FormState {
  name: string
  email: string
  phone: string
  message: string
  /** Honeypot: un bot que completa todos los inputs cae acá; un humano nunca lo ve. */
  website: string
}

const EMPTY_FORM: FormState = { name: '', email: '', phone: '', message: '', website: '' }

type FieldErrors = Partial<Record<'name' | 'message' | 'contact', string>>

function validate(form: FormState): FieldErrors {
  const errors: FieldErrors = {}
  if (!form.name.trim()) errors.name = 'Ingresá tu nombre.'
  if (!form.message.trim()) errors.message = 'Contanos brevemente qué estás buscando.'
  if (!form.email.trim() && !form.phone.trim()) {
    errors.contact = 'Dejanos un email o un teléfono para poder contactarte.'
  }
  return errors
}

/**
 * Formulario "Consultar por esta propiedad" (FichaPage, con `listingId`) / "Contacto" (HomePage,
 * sin `listingId`). Mismo componente en los dos lugares del sitio público: valida en el cliente,
 * manda la consulta anónima vía `publicApi` y no distingue para el usuario un 201 real de un 204
 * de honeypot — ambos se muestran como éxito.
 */
export function LeadForm({ slug, listingId, title = 'Dejanos tu consulta', description }: LeadFormProps) {
  const [form, setForm] = useState<FormState>(EMPTY_FORM)
  const [errors, setErrors] = useState<FieldErrors>({})
  const [status, setStatus] = useState<Status>('idle')
  const createLead = useCreatePublicLead(slug)

  function update<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((f) => ({ ...f, [key]: value }))
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    const fieldErrors = validate(form)
    setErrors(fieldErrors)
    if (Object.keys(fieldErrors).length > 0) return

    setStatus('sending')
    try {
      await createLead.mutateAsync({
        name: form.name.trim(),
        email: form.email.trim() || undefined,
        phone: form.phone.trim() || undefined,
        message: form.message.trim(),
        listingId,
        website: form.website || undefined,
      })
      setStatus('sent')
      setForm(EMPTY_FORM)
    } catch {
      setStatus('error')
    }
  }

  if (status === 'sent') {
    return (
      <div className="lead-form">
        <div className="lf-status ok" role="status">
          <CheckIcon size={18} />
          <div>
            <strong>¡Gracias por escribirnos!</strong>
            <div>Te contactamos a la brevedad.</div>
          </div>
        </div>
        <button type="button" className="lf-submit" onClick={() => setStatus('idle')}>
          Enviar otra consulta
        </button>
      </div>
    )
  }

  return (
    <form className="lead-form" onSubmit={handleSubmit} noValidate>
      {title && <h3>{title}</h3>}
      {description && <p>{description}</p>}

      {/* Honeypot: oculto con clip/posición (clase `visually-hidden` ya existe en publico.css),
          nunca con display:none — así un bot que sólo mira estilos aplicados sigue viéndolo y
          completándolo, que es justo lo que lo delata. */}
      <div className="visually-hidden" aria-hidden="true">
        <label htmlFor="lf-website">No completar este campo</label>
        <input
          id="lf-website"
          name="website"
          type="text"
          tabIndex={-1}
          autoComplete="off"
          value={form.website}
          onChange={(e) => update('website', e.target.value)}
        />
      </div>

      <div className="lf-field">
        <label htmlFor="lf-name">Nombre *</label>
        <input
          id="lf-name"
          value={form.name}
          onChange={(e) => update('name', e.target.value)}
          aria-invalid={!!errors.name}
          aria-describedby={errors.name ? 'lf-name-err' : undefined}
        />
        {errors.name && <span id="lf-name-err" className="lf-err">{errors.name}</span>}
      </div>

      <div className="lf-field">
        <label htmlFor="lf-email">Email</label>
        <input
          id="lf-email"
          type="email"
          value={form.email}
          onChange={(e) => update('email', e.target.value)}
        />
      </div>

      <div className="lf-field">
        <label htmlFor="lf-phone">Teléfono</label>
        <input
          id="lf-phone"
          type="tel"
          value={form.phone}
          onChange={(e) => update('phone', e.target.value)}
        />
      </div>
      {errors.contact && <span className="lf-err">{errors.contact}</span>}

      <div className="lf-field">
        <label htmlFor="lf-message">Mensaje *</label>
        <textarea
          id="lf-message"
          value={form.message}
          onChange={(e) => update('message', e.target.value)}
          aria-invalid={!!errors.message}
          aria-describedby={errors.message ? 'lf-message-err' : undefined}
        />
        {errors.message && <span id="lf-message-err" className="lf-err">{errors.message}</span>}
      </div>

      {status === 'error' && (
        <div className="lf-status error" role="alert">
          No pudimos enviar tu consulta. Probá de nuevo en unos minutos.
        </div>
      )}

      <button type="submit" className="lf-submit" disabled={status === 'sending'}>
        {status === 'sending' ? 'Enviando…' : 'Enviar consulta'}
      </button>
    </form>
  )
}
