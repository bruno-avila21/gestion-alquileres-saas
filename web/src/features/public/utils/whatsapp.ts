// Número de WhatsApp de la inmobiliaria, en E.164 sin "+". El de la demo es el que
// Palavecino y Asoc. publica en su propio sitio: (11) 5002-9352.
// TODO multi-tenant: cuando el alta de organización pida el teléfono, esto sale de
// `PublicOrgDto` en lugar de estar fijo. Con el valor vacío, "https://wa.me/?text=..."
// abre igual el selector de contacto (comportamiento oficial de wa.me sin número).
const WHATSAPP_PHONE = '5491150029352'

function waLink(text: string): string {
  return `https://wa.me/${WHATSAPP_PHONE}?text=${encodeURIComponent(text)}`
}

export function waConsultaPropiedad(code: string | null, title: string): string {
  const prefix = code ? `${code} — ` : ''
  return waLink(`Hola, consulto por la propiedad ${prefix}${title}`)
}

export function waGenerico(orgName: string): string {
  return waLink(`Hola, quería hacer una consulta a ${orgName}.`)
}

export function waTasacion(orgName: string): string {
  return waLink(`Hola, quiero coordinar una tasación con ${orgName}.`)
}
