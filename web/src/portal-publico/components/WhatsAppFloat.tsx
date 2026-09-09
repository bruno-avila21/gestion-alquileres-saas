import { waGenerico } from '@/features/public/utils/whatsapp'
import { WhatsAppIcon } from './icons'

export function WhatsAppFloat({ orgName }: { orgName: string }) {
  return (
    <a
      className="wa-float"
      href={waGenerico(orgName)}
      target="_blank"
      rel="noopener noreferrer"
      aria-label="Escribir por WhatsApp"
    >
      <span className="pulse" aria-hidden="true" />
      <WhatsAppIcon size={27} />
    </a>
  )
}
