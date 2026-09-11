import { Link } from 'react-router'
import type { PublicListingCard } from '@/features/public/types/public.types'
import { resolvePublicPhotoUrl } from '@/features/public/utils/resolvePublicPhotoUrl'
import { formatArea, operationLabel, propertyTypeLabel } from '@/features/public/utils/labels'
import { waConsultaPropiedad } from '@/features/public/utils/whatsapp'
import { locationLine } from '@/features/public/utils/location'
import { ChatIcon, NoPhotoIcon, PinIcon } from './icons'

const priceFormatter = new Intl.NumberFormat('es-AR')

/**
 * Las tres métricas de la franja del pie de la tarjeta. El modelo de diseño
 * pide exactamente tres columnas parejas, así que se eligen las tres más
 * informativas que la propiedad realmente tenga y se rellena con el tipo de
 * inmueble antes que dejar un hueco: una columna vacía rompe la grilla.
 */
function buildSpecs(listing: PublicListingCard): { k: string; v: string }[] {
  const specs: { k: string; v: string }[] = []
  const area = listing.coveredAreaM2 ?? listing.areaM2

  if (area) specs.push({ k: 'Superficie', v: formatArea(area) })
  if (listing.rooms) specs.push({ k: 'Ambientes', v: `${listing.rooms}` })
  if (listing.bedrooms) specs.push({ k: 'Dormitorios', v: `${listing.bedrooms}` })
  if (listing.bathrooms) specs.push({ k: 'Baños', v: `${listing.bathrooms}` })
  if (listing.garages) specs.push({ k: 'Cocheras', v: `${listing.garages}` })

  if (specs.length < 3) specs.push({ k: 'Tipo', v: propertyTypeLabel(listing.propertyType) })
  return specs.slice(0, 3)
}

export function ListingCard({ slug, listing }: { slug: string; listing: PublicListingCard }) {
  const isRent = listing.operationType !== 'Sale'
  const specs = buildSpecs(listing)
  const ficha = `/sitio/${slug}/propiedades/${listing.id}`

  return (
    <article className="card">
      <div className="card-media">
        {listing.coverPhotoUrl ? (
          <img
            src={resolvePublicPhotoUrl(listing.coverPhotoUrl)}
            alt={`${propertyTypeLabel(listing.propertyType)} en ${listing.neighborhood ?? listing.city}`}
            loading="lazy"
          />
        ) : (
          <div className="media-empty" role="img" aria-label="Sin fotos disponibles">
            <NoPhotoIcon />
            <span>Sin fotos</span>
          </div>
        )}
        <div className="card-tags">
          <span className={`tag ${isRent ? 'tag--rent' : 'tag--sale'}`}>{operationLabel(listing.operationType)}</span>
          {listing.isFeatured ? <span className="tag tag--featured">Destacada</span> : null}
        </div>
        {listing.code ? <span className="card-code">Cód. {listing.code}</span> : null}
      </div>

      <div className="card-body">
        <div className="price-row">
          <div className="price">
            <span className="cur">{listing.currency === 'USD' ? 'US$' : '$'}</span>
            {priceFormatter.format(listing.price)}
            {isRent ? <span className="per"> /mes</span> : null}
          </div>
          {listing.expenses ? (
            <span className="expenses">Exp. ${priceFormatter.format(listing.expenses)}</span>
          ) : null}
        </div>

        <h3>
          {/* Enlace estirado: cubre la tarjeta entera vía `.card-link::after`,
              sin envolver los botones de acción en un <a> anidado. */}
          <Link className="card-link" to={ficha}>{listing.title}</Link>
        </h3>

        <p className="addr">
          <PinIcon />
          {locationLine(listing.neighborhood, listing.city)}
        </p>

        <div className="specs">
          {specs.map((s) => (
            <div className="spec" key={s.k}>
              <span className="k">{s.k}</span>
              <span className="v">{s.v}</span>
            </div>
          ))}
        </div>

        <div className="card-actions">
          <a
            className="btn btn--soft"
            href={waConsultaPropiedad(listing.code, listing.title)}
            target="_blank"
            rel="noopener noreferrer"
          >
            <ChatIcon size={17} />
            Consultar
          </a>
          <Link className="btn btn--primary" to={ficha} tabIndex={-1} aria-hidden="true">Ficha</Link>
        </div>
      </div>
    </article>
  )
}

export function ListingCardSkeleton() {
  return (
    <div className="card" aria-hidden="true">
      <div className="card-media skeleton skel-card" />
      <div className="card-body">
        <div className="skeleton" style={{ height: 24, width: '55%', borderRadius: 6 }} />
        <div className="skeleton" style={{ height: 18, width: '85%', borderRadius: 6 }} />
        <div className="skeleton" style={{ height: 14, width: '60%', borderRadius: 6 }} />
        <div className="skeleton" style={{ height: 52, width: '100%', borderRadius: 6, marginTop: 'auto' }} />
      </div>
    </div>
  )
}
