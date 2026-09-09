import { useState } from 'react'
import { useNavigate, useOutletContext } from 'react-router'
import { usePublicListings } from '@/features/public/hooks/usePublic'
import type { PublicOperationType, PublicPropertyType } from '@/features/public/types/public.types'
import { PROPERTY_TYPE_LABELS } from '@/features/public/utils/labels'
import { LeadForm } from '@/features/public/components/LeadForm'
import type { PublicoOutletContext } from '../types'
import { ListingCard, ListingCardSkeleton } from '../components/ListingCard'
import { ArrowRightIcon, SearchIcon } from '../components/icons'

const FEATURED_PAGE_SIZE = 6

export default function HomePage() {
  const { org, slug } = useOutletContext<PublicoOutletContext>()
  const navigate = useNavigate()

  const [operation, setOperation] = useState<'' | PublicOperationType>('')
  const [type, setType] = useState<'' | PublicPropertyType>('')
  const [zone, setZone] = useState('')

  const { data, isLoading, isError } = usePublicListings(slug, { pageSize: FEATURED_PAGE_SIZE })
  const neighborhoods = data?.facets.neighborhoods ?? []

  function handleSearch(e: React.FormEvent) {
    e.preventDefault()
    const params = new URLSearchParams()
    if (operation) params.set('operation', operation)
    if (type) params.set('type', type)
    const match = neighborhoods.find((n) => n.value.toLowerCase() === zone.trim().toLowerCase())
    if (match) params.set('neighborhood', match.value)
    navigate(`/sitio/${slug}/propiedades${params.toString() ? `?${params}` : ''}`)
  }

  return (
    <>
      <section className="hero">
        <div className="wrap">
          <div className="eyebrow">{org.name}</div>
          <h1>Tu próxima propiedad, sin dar tantas vueltas.</h1>
          <p>Propiedades en venta y alquiler, con fichas claras y contacto directo por WhatsApp. Buscá por zona, precio y ambientes.</p>
          <form className="searchbar" role="search" onSubmit={handleSearch}>
            <div className="field">
              <label htmlFor="h-op">Operación</label>
              <select id="h-op" value={operation} onChange={(e) => setOperation(e.target.value as '' | PublicOperationType)}>
                <option value="">Venta y alquiler</option>
                <option value="Sale">Venta</option>
                <option value="Rent">Alquiler</option>
              </select>
            </div>
            <div className="field">
              <label htmlFor="h-type">Tipo</label>
              <select id="h-type" value={type} onChange={(e) => setType(e.target.value as '' | PublicPropertyType)}>
                <option value="">Todos</option>
                {Object.entries(PROPERTY_TYPE_LABELS).map(([value, label]) => (
                  <option key={value} value={value}>{label}</option>
                ))}
              </select>
            </div>
            <div className="field">
              <label htmlFor="h-zone">Zona</label>
              <input
                id="h-zone"
                placeholder="Barrio o localidad"
                list="pp-zones"
                value={zone}
                onChange={(e) => setZone(e.target.value)}
              />
              <datalist id="pp-zones">
                {neighborhoods.map((n) => (
                  <option key={n.value} value={n.value} />
                ))}
              </datalist>
            </div>
            <button className="search-btn" type="submit">
              <SearchIcon />
              Buscar
            </button>
          </form>
        </div>
      </section>

      <section className="section wrap">
        <div className="section-head">
          <div>
            <div className="kicker">Selección de la semana</div>
            <h2>Propiedades destacadas</h2>
            <p>Una muestra de la cartera. Tocá cualquiera para ver la ficha completa.</p>
          </div>
          <button type="button" className="link-btn" onClick={() => navigate(`/sitio/${slug}/propiedades`)}>
            Ver todas
            <ArrowRightIcon />
          </button>
        </div>
        {isError ? (
          <div className="state-box">No pudimos cargar las propiedades destacadas.</div>
        ) : (
          <div className="grid">
            {isLoading
              ? Array.from({ length: FEATURED_PAGE_SIZE }).map((_, i) => <ListingCardSkeleton key={i} />)
              : data?.items.map((listing) => <ListingCard key={listing.id} slug={slug} listing={listing} />)}
          </div>
        )}
      </section>

      <section className="section wrap" id="contacto">
        <div className="section-head">
          <div>
            <div className="kicker">Contacto</div>
            <h2>¿Buscás algo puntual?</h2>
            <p>Contanos qué necesitás y te escribimos a la brevedad.</p>
          </div>
        </div>
        <LeadForm slug={slug} description="No hace falta que sea sobre una propiedad publicada: contanos qué buscás." />
      </section>
    </>
  )
}
