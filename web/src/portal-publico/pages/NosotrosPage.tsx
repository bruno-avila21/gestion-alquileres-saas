import { useOutletContext } from 'react-router'
import type { PublicoOutletContext } from '../types'

export default function NosotrosPage() {
  const { org } = useOutletContext<PublicoOutletContext>()

  return (
    <div className="wrap simple-page">
      <div className="kicker">La empresa</div>
      <h1>{org.name}</h1>
      {/* `white-space: pre-line` en `.simple-page p` ya respeta los saltos de línea que
          escriba la inmobiliaria, así que puede armar varios párrafos sin editor de texto rico. */}
      <p>
        {org.site.aboutText
          ?? 'Acompañamos a propietarios e inquilinos en cada etapa de la operación: venta, alquiler y tasaciones, con fichas claras y contacto directo por WhatsApp para resolver dudas sin vueltas.'}
      </p>
    </div>
  )
}
