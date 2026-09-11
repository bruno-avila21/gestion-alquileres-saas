import { useState } from 'react'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { IcCheck, IcMail, IcPhone } from '@/shared/components/ui/Icons'
import { useAuthStore } from '@/shared/stores/authStore'
import { useOrganization } from '@/features/organization/hooks/useOrganization'

/**
 * Canales de soporte del producto. Van por variable de entorno y no hardcodeados: son
 * datos de quien opera la plataforma, no de la inmobiliaria, y cambian según el
 * despliegue. Sin configurar, la pantalla lo dice en vez de mostrar un link muerto.
 */
const SOPORTE_EMAIL = import.meta.env.VITE_SUPPORT_EMAIL as string | undefined
const SOPORTE_WHATSAPP = import.meta.env.VITE_SUPPORT_WHATSAPP as string | undefined

/** Lo que soporte pregunta siempre y nadie tiene a mano cuando escribe enojado. */
function useDiagnostico() {
  const user = useAuthStore((s) => s.user)
  const { data: org } = useOrganization()

  return [
    ['Organización', org?.name ?? user?.organizationSlug ?? '—'],
    ['Identificador', user?.organizationSlug ?? '—'],
    ['ID de organización', user?.organizationId ?? '—'],
    ['Usuario', user?.email ?? '—'],
    ['Rol', user?.role ?? '—'],
    ['Plan', org?.plan ?? '—'],
    ['Pantalla', window.location.pathname],
    ['Navegador', navigator.userAgent],
  ] as const
}

export default function SoportePage() {
  const diagnostico = useDiagnostico()
  const [copiado, setCopiado] = useState(false)

  const textoDiagnostico = diagnostico.map(([k, v]) => `${k}: ${v}`).join('\n')

  async function copiar() {
    try {
      await navigator.clipboard.writeText(textoDiagnostico)
      setCopiado(true)
      setTimeout(() => setCopiado(false), 2500)
    } catch {
      // Sin permiso de portapapeles (o contexto no seguro): el bloque de abajo se puede
      // seleccionar y copiar a mano, así que no hace falta avisar nada.
    }
  }

  const asunto = encodeURIComponent(`Soporte — ${diagnostico[1][1]}`)
  const cuerpo = encodeURIComponent(`Contanos qué pasó:\n\n\n---\n${textoDiagnostico}`)

  return (
    <>
      <AdminTopbar crumbs={['Soporte']} />
      <div className="page">
        <div className="page-h">
          <div>
            <h1>Soporte</h1>
            <div className="lead">Escribinos y te respondemos. Antes, mirá si es algo de acá abajo.</div>
          </div>
        </div>

        <div style={{ maxWidth: 720, display: 'flex', flexDirection: 'column', gap: 'var(--s-7)' }}>
          <div className="card">
            <div className="card-h"><h3>Escribinos</h3></div>
            <div className="card-b" style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              {SOPORTE_EMAIL || SOPORTE_WHATSAPP ? (
                <>
                  <div style={{ fontSize: 'var(--fs-sm)', color: 'var(--muted)', lineHeight: 1.5 }}>
                    El mensaje ya sale con los datos técnicos de abajo adjuntos: no hace falta
                    que los copies vos.
                  </div>
                  <div className="row" style={{ gap: 8, flexWrap: 'wrap' }}>
                    {SOPORTE_EMAIL ? (
                      <a className="btn btn--primary" href={`mailto:${SOPORTE_EMAIL}?subject=${asunto}&body=${cuerpo}`}>
                        <IcMail size={14} /> Escribir por email
                      </a>
                    ) : null}
                    {SOPORTE_WHATSAPP ? (
                      <a
                        className="btn"
                        href={`https://wa.me/${SOPORTE_WHATSAPP}?text=${asunto}`}
                        target="_blank"
                        rel="noopener noreferrer"
                      >
                        <IcPhone size={14} /> Escribir por WhatsApp
                      </a>
                    ) : null}
                  </div>
                </>
              ) : (
                <div style={{ fontSize: 'var(--fs-sm)', color: 'var(--muted)', lineHeight: 1.5 }}>
                  Todavía no hay un canal de soporte configurado en este despliegue. Se define con
                  las variables <code>VITE_SUPPORT_EMAIL</code> y <code>VITE_SUPPORT_WHATSAPP</code>.
                  Mientras tanto, copiá los datos de abajo y mandalos por el canal que uses hoy.
                </div>
              )}
            </div>
          </div>

          <div className="card">
            <div className="card-h">
              <div>
                <h3>Datos técnicos</h3>
                <div className="sub">lo que vamos a preguntarte igual</div>
              </div>
              <button className="btn btn--sm" onClick={copiar}>
                {copiado ? <><IcCheck size={12} /> Copiado</> : 'Copiar'}
              </button>
            </div>
            <div className="card-b">
              <dl style={{ display: 'grid', gridTemplateColumns: 'auto 1fr', gap: '8px 20px', margin: 0 }}>
                {diagnostico.map(([k, v]) => (
                  <div key={k} style={{ display: 'contents' }}>
                    <dt style={{ fontSize: 'var(--fs-sm)', color: 'var(--muted)', whiteSpace: 'nowrap' }}>{k}</dt>
                    <dd
                      className="mono"
                      style={{
                        margin: 0, fontSize: 'var(--fs-xs)', wordBreak: 'break-all',
                        color: 'var(--ink)',
                      }}
                    >
                      {v}
                    </dd>
                  </div>
                ))}
              </dl>
            </div>
          </div>

          <div className="card">
            <div className="card-h"><h3>Antes de escribir</h3></div>
            <div className="card-b">
              <ul style={{ margin: 0, paddingLeft: 18, display: 'flex', flexDirection: 'column', gap: 10, fontSize: 'var(--fs-sm)', lineHeight: 1.55 }}>
                <li>
                  <b>No entra un usuario.</b> El login pide tres datos, no dos: además del email y
                  la contraseña, el identificador de la inmobiliaria. Si alguno no coincide, el
                  mensaje es el mismo a propósito.
                </li>
                <li>
                  <b>Un ajuste no se aplicó.</b> Mirá en Índices si el período ya tiene el valor
                  publicado por el BCRA o el INDEC. Sin el índice cargado, el ajuste espera.
                </li>
                <li>
                  <b>Un propietario no aparece en la liquidación.</b> Entra sólo si tuvo cobranzas
                  acreditadas en el período. Y para transferirle hace falta su CBU cargado.
                </li>
                <li>
                  <b>No llegan los avisos por email.</b> El envío depende del proveedor de correo
                  del despliegue. Si no está configurado, los avisos quedan registrados pero no
                  salen.
                </li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </>
  )
}
