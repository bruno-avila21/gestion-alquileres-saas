import { useState } from 'react'
import { Link, useOutletContext, useParams } from 'react-router'
import { usePublicListing } from '@/features/public/hooks/usePublic'
import { LeadForm } from '@/features/public/components/LeadForm'
import { resolvePublicPhotoUrl } from '@/features/public/utils/resolvePublicPhotoUrl'
import { formatArea, operationLabel, propertyTypeLabel } from '@/features/public/utils/labels'
import { waConsultaPropiedad } from '@/features/public/utils/whatsapp'
import type { PublicoOutletContext } from '../types'
import { BackArrowIcon, NoPhotoIcon, PinIcon, WhatsAppIcon } from '../components/icons'
import { PublicoNotFound } from './PublicoNotFound'

export default function FichaPage() {
  const { slug } = useOutletContext<PublicoOutletContext>()
  const { id } = useParams<{ id: string }>()
  const { data: p, isLoading, isError } = usePublicListing(slug, id)
  const [activePhoto, setActivePhoto] = useState(0)

  if (isLoading) {
    return (
      <div className="wrap detail">
        <div className="state-box" role="status">
          <div className="spinner" />
          Cargando propiedad…
        </div>
      </div>
    )
  }

  if (isError || !p) {
    return (
      <div className="wrap detail">
        <PublicoNotFound title="Propiedad no encontrada" message="Puede que ya no esté disponible o el link sea incorrecto." />
      </div>
    )
  }

  const isRent = p.operationType !== 'Sale'
  const photos = p.photoUrls.length ? p.photoUrls : []
  const mainPhoto = photos[activePhoto] ?? photos[0]

  const attrs: { k: string; v: string }[] = []
  const push = (k: string, v: string | number | null | undefined) => {
    if (v !== null && v !== undefined && v !== '') attrs.push({ k, v: String(v) })
  }
  push('Ambientes', p.rooms)
  push('Dormitorios', p.bedrooms)
  push('Baños', p.bathrooms)
  push('Cocheras', p.garages)
  push('Cubierta', p.coveredAreaM2 ? formatArea(p.coveredAreaM2) : null)
  push('Total', p.areaM2 ? formatArea(p.areaM2) : null)
  push('Antigüedad', p.ageYears != null ? (p.ageYears === 0 ? 'A estrenar' : `${p.ageYears} años`) : null)
  push('Apto crédito', p.suitableForCredit == null ? null : p.suitableForCredit ? 'Sí' : 'No')

  return (
    <div className="wrap detail">
      <Link className="back" to={`/sitio/${slug}/propiedades`}>
        <BackArrowIcon />
        Volver al listado
      </Link>

      <div className="detail-grid">
        <div>
          <div className="gallery-main">
            {mainPhoto ? (
              <img src={resolvePublicPhotoUrl(mainPhoto)} alt={p.title} />
            ) : (
              <div className="media-empty" role="img" aria-label="Sin fotos disponibles">
                <NoPhotoIcon size={44} />
                <span>Sin fotos disponibles</span>
              </div>
            )}
          </div>
          {photos.length > 1 ? (
            <div className="thumbs">
              {photos.map((url, i) => (
                <button
                  key={url}
                  type="button"
                  className={i === activePhoto ? 'on' : undefined}
                  onClick={() => setActivePhoto(i)}
                  aria-label={`Ver foto ${i + 1}`}
                  aria-current={i === activePhoto}
                >
                  <img src={resolvePublicPhotoUrl(url)} alt={`Foto ${i + 1} de ${p.title}`} />
                </button>
              ))}
            </div>
          ) : null}

          <div className="dcontent">
            <div style={{ display: 'flex', gap: 9, alignItems: 'center', marginBottom: 8 }}>
              <span className={`badge ${isRent ? 'rent' : 'sale'}`} style={{ position: 'static' }}>{operationLabel(p.operationType)}</span>
              <span className="mono" style={{ fontSize: 12, color: 'var(--faint)' }}>
                {propertyTypeLabel(p.propertyType)} · {p.code ?? ''}
              </span>
            </div>
            <h2>{p.title}</h2>
            <div className="addr" style={{ marginTop: 8, fontSize: 15 }}>
              <PinIcon />
              {p.address ? `${p.address} · ` : ''}
              {p.neighborhood ? `${p.neighborhood}, ` : ''}
              {p.city}
            </div>

            <div className="dattrs">
              {attrs.map((a) => (
                <div className="dattr" key={a.k}>
                  <span className="k">{a.k}</span>
                  <span className="v">{a.v}</span>
                </div>
              ))}
            </div>

            {p.description ? (
              <>
                <h4>Descripción</h4>
                <p>{p.description}</p>
              </>
            ) : null}

            {p.features.length ? (
              <>
                <h4>Servicios y características</h4>
                <ul className="feature-list">
                  {p.features.map((f) => (
                    <li key={f}>
                      <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2.4} aria-hidden="true">
                        <path d="M20 6 9 17l-5-5" />
                      </svg>
                      {f}
                    </li>
                  ))}
                </ul>
              </>
            ) : null}

            <div className="mapbox">
              <span className="pin" aria-hidden="true">
                <svg width="30" height="30" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M12 2a7 7 0 0 0-7 7c0 5 7 13 7 13s7-8 7-13a7 7 0 0 0-7-7zm0 9.5A2.5 2.5 0 1 1 12 6.5a2.5 2.5 0 0 1 0 5z" />
                </svg>
              </span>
              <span className="maplabel">
                <PinIcon />
                {p.neighborhood ?? p.city} — ubicación aproximada
              </span>
            </div>
          </div>
        </div>

        <div>
          <div className="contact-card">
            <div className="cc-head">
              <div className="op">{operationLabel(p.operationType)}</div>
              <div className="p">
                <span className="cur">{p.currency === 'USD' ? 'US$' : '$'}</span>
                {new Intl.NumberFormat('es-AR').format(p.price)}
                {isRent ? <span className="cur"> /mes</span> : null}
              </div>
              {p.expenses ? <div className="exp">+ ${new Intl.NumberFormat('es-AR').format(p.expenses)} de expensas</div> : null}
            </div>
            <div className="cc-body">
              <div className="code">Cód. {p.code ?? '—'}</div>
              <a className="btn btn-wa" href={waConsultaPropiedad(p.code, p.title)} target="_blank" rel="noopener noreferrer">
                <WhatsAppIcon />
                Consultar por WhatsApp
              </a>
              <Link className="btn btn-ghost" to={`/sitio/${slug}/contacto`}>Solicitar visita</Link>
            </div>
          </div>

          <div className="lead-card">
            <LeadForm
              slug={slug}
              listingId={p.id}
              title="Consultar por esta propiedad"
              description="Dejanos tus datos y te contactamos con más información."
            />
          </div>
        </div>
      </div>
    </div>
  )
}
