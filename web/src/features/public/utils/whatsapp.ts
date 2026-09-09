// TODO: reemplazar por el número real de WhatsApp de la inmobiliaria (formato E.164 sin
// "+", ej. "5491150029352") cuando el cliente lo confirme. Con el placeholder vacío,
// "https://wa.me/?text=..." abre igual el selector de contacto de WhatsApp (comportamiento
// oficial de wa.me sin número), así que el botón funciona también antes de tener el dato.
const WHATSAPP_PHONE = ''

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
