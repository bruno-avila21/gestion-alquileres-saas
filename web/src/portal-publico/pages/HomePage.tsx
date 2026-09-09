import { useState } from 'react'
import { useNavigate, useOutletContext } from 'react-router'
import { usePublicListings } from '@/features/public/hooks/usePublic'
import type { PublicOperationType, PublicPropertyType } from '@/features/public/types/public.types'
import { PROPERTY_TYPE_LABELS } from '@/features/public/utils/labels'
import { resolvePublicPhotoUrl } from '@/features/public/utils/resolvePublicPhotoUrl'
import { LeadForm } from '@/features/public/components/LeadForm'
import type { PublicoOutletContext } from '../types'
import { ListingCard, ListingCardSkeleton } from '../components/ListingCard'
import {
  ArrowRightIcon, BuildingIcon, ChartIcon, KeyIcon, PinIcon,
  SearchIcon, ShieldIcon, TagIcon,
} from '../components/icons'

const FEATURED_PAGE_SIZE = 6
const OPERATION_TABS: { value: '' | PublicOperationType; label: string }[] = [
  { value: '', label: 'Todas' },
  { value: 'Sale', label: 'Comprar' },
  { value: 'Rent', label: 'Alquilar' },
]

const SERVICES = [
  {
    icon: ChartIcon,
    title: 'Tasación con respaldo',
    body: 'Valuamos con comparables reales de la zona y te entregamos el informe por escrito, sin compromiso.',
  },
  {
    icon: KeyIcon,
    title: 'Administración de alquileres',
    body: 'Cobranza, recibos y rendición al propietario. Los ajustes ICL e IPC se calculan solos y se avisan a tiempo.',
  },
  {
    icon: ShieldIcon,
    title: 'Contratos y garantías',
    body: 'Analizamos la garantía, redactamos el contrato y acompañamos la firma de las dos partes.',
  },
]

export default function HomePage() {
  const { org, slug } = useOutletContext<PublicoOutletContext>()
  const navigate = useNavigate()

  const [operation, setOperation] = useState<'' | PublicOperationType>('')
  const [type, setType] = useState<'' | PublicPropertyType>('')
  const [zone, setZone] = useState('')
  const [maxPrice, setMaxPrice] = useState('')

  const { data, isLoading, isError } = usePublicListings(slug, { pageSize: FEATURED_PAGE_SIZE })
  const neighborhoods = data?.facets.neighborhoods ?? []

  // La foto del hero sale de la cartera: siempre muestra una propiedad real y
  // vigente, sin depender de un asset genérico de stock que envejece mal.
  const heroPhoto = data?.items.find((i) => i.coverPhotoUrl)?.coverPhotoUrl ?? null

  const facetCount = (list: { value: string; count: number }[], value: string) =>
    list.find((f) => f.value === value)?.count ?? 0
  const ventas = facetCount(data?.facets.operationTypes ?? [], 'Sale')
  const alquileres = (data?.facets.operationTypes ?? [])
    .filter((f) => f.value !== 'Sale')
    .reduce((acc, f) => acc + f.count, 0)

  function goToListado(extra: Record<string, string> = {}) {
    const params = new URLSearchParams()
    if (operation) params.set('operation', operation)
    if (type) params.set('type', type)
    const match = neighborhoods.find((n) => n.value.toLowerCase() === zone.trim().toLowerCase())
    if (match) params.set('neighborhood', match.value)
    const max = Number(maxPrice.replace(/\D/g, ''))
    if (max > 0) params.set('maxPrice', String(max))
    for (const [k, v] of Object.entries(extra)) params.set(k, v)
    navigate(`/sitio/${slug}/propiedades${params.toString() ? `?${params}` : ''}`)
  }

  function handleSearch(e: React.FormEvent) {
    e.preventDefault()
    goToListado()
  }

  const metrics = [
    { icon: BuildingIcon, v: data ? `${data.total}` : '—', k: 'Propiedades activas' },
    { icon: TagIcon, v: ventas ? `${ventas}` : '—', k: 'En venta' },
    { icon: KeyIcon, v: alquileres ? `${alquileres}` : '—', k: 'En alquiler' },
    { icon: PinIcon, v: neighborhoods.length ? `${neighborhoods.length}` : '—', k: 'Zonas cubiertas' },
  ]

  return (
    <>
      <section className="hero">
        <div className="hero-bg" aria-hidden="true">
          {heroPhoto ? <img src={resolvePublicPhotoUrl(heroPhoto)} alt="" /> : null}
        </div>

        <div className="wrap">
          {neighborhoods.length ? (
            <div className="hero-pill">
              <span className="live" aria-hidden="true" />
              {neighborhoods.slice(0, 4).map((n) => n.value).join(' · ')}
            </div>
          ) : null}

          <h1>Encontrá tu próxima propiedad con quien conoce la zona.</h1>
          <p className="lede">
            Venta y alquiler en {org.name}, con fichas claras, precios al día y contacto directo.
            Si administramos tu contrato, los <b>ajustes ICL e IPC</b> se calculan y se avisan solos.
          </p>

          <form className="searchcard" role="search" onSubmit={handleSearch}>
            <div className="search-tabs">
              <div className="seg" role="group" aria-label="Tipo de operación">
                {OPERATION_TABS.map((tab) => (
                  <button
                    key={tab.label}
                    type="button"
                    className={operation === tab.value ? 'on' : undefined}
                    aria-pressed={operation === tab.value}
                    onClick={() => setOperation(tab.value)}
                  >
                    {tab.label}
                  </button>
                ))}
              </div>
              <span className="search-hint">
                {data ? `${data.total} propiedades publicadas` : 'Cargando cartera…'}
              </span>
            </div>

            <div className="search-grid">
              <div className="field">
                <label htmlFor="h-zone">Ubicación o barrio</label>
                <div className="ctrl">
                  <PinIcon size={17} />
                  <input
                    id="h-zone"
                    placeholder="ej: Bella Vista, Villa Pueyrredón…"
                    list="pp-zones"
                    value={zone}
                    onChange={(e) => setZone(e.target.value)}
                  />
                </div>
                <datalist id="pp-zones">
                  {neighborhoods.map((n) => (
                    <option key={n.value} value={n.value} />
                  ))}
                </datalist>
              </div>

              <div className="field">
                <label htmlFor="h-type">Tipo de inmueble</label>
                <div className="ctrl">
                  <BuildingIcon size={17} />
                  <select id="h-type" value={type} onChange={(e) => setType(e.target.value as '' | PublicPropertyType)}>
                    <option value="">Todas las tipologías</option>
                    {Object.entries(PROPERTY_TYPE_LABELS).map(([value, label]) => (
                      <option key={value} value={value}>{label}</option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="field">
                <label htmlFor="h-max">Presupuesto máximo</label>
                <div className="ctrl">
                  <TagIcon size={17} />
                  <input
                    id="h-max"
                    inputMode="numeric"
                    placeholder="Sin tope"
                    value={maxPrice}
                    onChange={(e) => setMaxPrice(e.target.value)}
                  />
                </div>
              </div>

              <button className="search-btn" type="submit">
                <SearchIcon />
                Buscar
              </button>
            </div>

            {neighborhoods.length ? (
              <div className="quicktags">
                <span className="qt-label">Búsquedas frecuentes:</span>
                {neighborhoods.slice(0, 4).map((n) => (
                  <button key={n.value} type="button" onClick={() => goToListado({ neighborhood: n.value })}>
                    {n.value} ({n.count})
                  </button>
                ))}
              </div>
            ) : null}
          </form>
        </div>
      </section>

      <section className="trustband">
        <div className="wrap">
          <div className="cols">
            {metrics.map(({ icon: Icon, v, k }) => (
              <div className="metric" key={k}>
                <span className="ico" aria-hidden="true"><Icon size={22} /></span>
                <div>
                  <div className="v num">{v}</div>
                  <div className="k">{k}</div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="section">
        <div className="wrap">
          <div className="section-head">
            <div>
              <div className="kicker">Cartera seleccionada</div>
              <h2>Propiedades destacadas</h2>
              <p>Una muestra de lo que tenemos publicado. Entrá a cualquiera para ver la ficha completa.</p>
            </div>
            <div className="head-aside">
              {data ? (
                <span className="count num">
                  Mostrando {data.items.length} de {data.total}
                </span>
              ) : null}
              <button type="button" className="link-btn" onClick={() => navigate(`/sitio/${slug}/propiedades`)}>
                Ver catálogo completo
                <ArrowRightIcon />
              </button>
            </div>
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
        </div>
      </section>

      <section className="section section--alt">
        <div className="wrap">
          <div className="section-head">
            <div>
              <div className="kicker">Servicios</div>
              <h2>Más que publicar una propiedad</h2>
              <p>Acompañamos la operación completa, desde la tasación hasta la rendición mensual al propietario.</p>
            </div>
          </div>
          <div className="svc-grid">
            {SERVICES.map(({ icon: Icon, title, body }) => (
              <article className="svc" key={title}>
                <span className="ico" aria-hidden="true"><Icon size={22} /></span>
                <h3>{title}</h3>
                <p>{body}</p>
              </article>
            ))}
          </div>
        </div>
      </section>

      <section className="section" id="contacto">
        <div className="wrap">
          <div className="section-head">
            <div>
              <div className="kicker">Contacto</div>
              <h2>¿Buscás algo puntual?</h2>
              <p>Contanos qué necesitás y te escribimos a la brevedad.</p>
            </div>
          </div>
          <LeadForm slug={slug} description="No hace falta que sea sobre una propiedad publicada: contanos qué buscás." />
        </div>
      </section>
    </>
  )
}
