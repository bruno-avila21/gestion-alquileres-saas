import { Link } from 'react-router'
import type { PublicListingCard } from '@/features/public/types/public.types'
import { resolvePublicPhotoUrl } from '@/features/public/utils/resolvePublicPhotoUrl'
import { formatArea, operationLabel, propertyTypeLabel } from '@/features/public/utils/labels'
import { AreaIcon, BathIcon, BedIcon, NoPhotoIcon, PinIcon, RoomsIcon } from './icons'

export function ListingCard({ slug, listing }: { slug: string; listing: PublicListingCard }) {
  const isRent = listing.operationType !== 'Sale'
  const coverAreaM2 = listing.coveredAreaM2 ?? listing.areaM2

  return (
    <Link to={`/sitio/${slug}/propiedades/${listing.id}`} className="card" aria-label={`Ver ficha de ${listing.title}`}>
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
        <span className={`badge ${isRent ? 'rent' : 'sale'}`}>{operationLabel(listing.operationType)}</span>
      </div>
      <div className="card-body">
        <div className="price-row">
          <div className="price">
            <span className="cur">{listing.currency === 'USD' ? 'US$' : '$'}</span>
            {new Intl.NumberFormat('es-AR').format(listing.price)}
            {isRent ? <span className="per"> /mes</span> : null}
          </div>
          <div className="type-code">
            <b>{propertyTypeLabel(listing.propertyType)}</b>
            {listing.code ?? ''}
          </div>
        </div>
        <h3>{listing.title}</h3>
        <div className="addr">
          <PinIcon />
          {listing.neighborhood ? `${listing.neighborhood}, ` : ''}
          {listing.city}
        </div>
        <div className="specs">
          {listing.rooms ? (
            <span className="spec">
              <RoomsIcon />
              {listing.rooms} amb
            </span>
          ) : null}
          {listing.bedrooms ? (
            <span className="spec">
              <BedIcon />
              {listing.bedrooms} dorm
            </span>
          ) : null}
          {listing.bathrooms ? (
            <span className="spec">
              <BathIcon />
              {listing.bathrooms} baño{listing.bathrooms > 1 ? 's' : ''}
            </span>
          ) : null}
          {coverAreaM2 ? (
            <span className="spec">
              <AreaIcon />
              {formatArea(coverAreaM2)}
            </span>
          ) : null}
        </div>
      </div>
    </Link>
  )
}

export function ListingCardSkeleton() {
  return (
    <div className="card" aria-hidden="true">
      <div className="card-media skeleton skel-card" />
      <div className="card-body">
        <div className="skeleton" style={{ height: 22, width: '55%', borderRadius: 6 }} />
        <div className="skeleton" style={{ height: 16, width: '80%', borderRadius: 6 }} />
        <div className="skeleton" style={{ height: 13, width: '60%', borderRadius: 6 }} />
      </div>
    </div>
  )
}
