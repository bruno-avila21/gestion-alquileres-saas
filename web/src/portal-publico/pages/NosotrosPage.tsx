import { useOutletContext } from 'react-router'
import type { PublicoOutletContext } from '../types'

export default function NosotrosPage() {
  const { org } = useOutletContext<PublicoOutletContext>()

  return (
    <div className="wrap simple-page">
      <div className="kicker">La empresa</div>
      <h1>{org.name}</h1>
      <p>
        Acompañamos a propietarios e inquilinos en cada etapa de la operación: venta, alquiler y
        tasaciones, con fichas claras y contacto directo por WhatsApp para resolver dudas sin
        vueltas.
      </p>
    </div>
  )
}
