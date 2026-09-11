import { useOutletContext } from 'react-router'
import type { PublicoOutletContext } from '../types'
import { waGenerico } from '@/features/public/utils/whatsapp'
import { WhatsAppIcon } from '../components/icons'

export default function ContactoPage() {
  const { org } = useOutletContext<PublicoOutletContext>()

  return (
    <div className="wrap simple-page">
      <div className="kicker">Contacto</div>
      <h1>Hablemos de tu próxima propiedad</h1>
      <p>
        Escribinos por WhatsApp y te respondemos a la brevedad, o dejanos tu consulta y nuestro
        equipo se pone en contacto con vos.
      </p>
      <div className="contact-info">
        <a className="btn btn-wa" style={{ width: 'fit-content', padding: '13px 22px' }} href={waGenerico(org.name)} target="_blank" rel="noopener noreferrer">
          <WhatsAppIcon />
          Escribir por WhatsApp
        </a>
      </div>
    </div>
  )
}
