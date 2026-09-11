import { useState } from 'react'
import { Link, useNavigate, useOutletContext } from 'react-router'
import { usePublicListings } from '@/features/public/hooks/usePublic'
import type { PublicCurrency, PublicOperationType, PublicPropertyType } from '@/features/public/types/public.types'
import { PROPERTY_TYPE_LABELS } from '@/features/public/utils/labels'
import { publicLogoUrl, resolvePublicPhotoUrl } from '@/features/public/utils/resolvePublicPhotoUrl'
import { waTasacion } from '@/features/public/utils/whatsapp'
import { LeadForm } from '@/features/public/components/LeadForm'
import type { PublicoOutletContext } from '../types'
import { ListingCard, ListingCardSkeleton } from '../components/ListingCard'
import {
  ArrowRightIcon, BuildingIcon, ChartIcon, ChatIcon, CheckIcon, KeyIcon, MailIcon, PhoneIcon,
  PinIcon, SearchIcon, ShieldIcon, TagIcon,
} from '../components/icons'

const FEATURED_PAGE_SIZE = 6
const OPERATION_TABS: { value: '' | PublicOperationType; label: string }[] = [
  { value: '', label: 'Todas' },
  { value: 'Sale', label: 'Comprar' },
  { value: 'Rent', label: 'Alquilar' },
]

/** Lo que la inmobiliaria garantiza en una tasación. Texto del modelo de diseño. */
const TASACION_CLAIMS = ['Sin costo inicial', 'Informe por escrito', 'Presencial o digital']

/** Compromisos del panel institucional. Describen lo que el producto habilita, no títulos
 *  ni matrículas: son el texto por defecto de CUALQUIER inmobiliaria del SaaS. */
const IDENTITY_PLEDGES = [
  'Contrato redactado y firma acompañada',
  'Rendición mensual al propietario, con recibo',
  'Ajustes ICL e IPC calculados y avisados a tiempo',
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
  // Sin moneda, un presupuesto de "130.000" mezcla dólares con pesos y el listado
  // devuelve cualquier cosa. Por eso la moneda viaja al lado del importe, no aparte.
  const [currency, setCurrency] = useState<'' | PublicCurrency>('')

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
    if (currency) params.set('currency', currency)
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

          {/* Los textos de portada los edita la inmobiliaria desde el panel. Sin tocar nada,
              quedan los del diseño — un sitio recién creado ya está terminado. */}
          <h1>{org.site.heroTitle ?? 'Encontrá tu próxima propiedad con quien conoce la zona.'}</h1>
          {org.site.heroSubtitle ? (
            <p className="lede">{org.site.heroSubtitle}</p>
          ) : (
            <p className="lede">
              Venta y alquiler en {org.name}, con fichas claras, precios al día y contacto directo.
              Si administramos tu contrato, los <b>ajustes ICL e IPC</b> se calculan y se avisan solos.
            </p>
          )}

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
                <div className="ctrl ctrl--money">
                  <TagIcon size={17} />
                  <input
                    id="h-max"
                    inputMode="numeric"
                    placeholder="Sin tope"
                    value={maxPrice}
                    onChange={(e) => setMaxPrice(e.target.value)}
                  />
                  <select
                    className="cur-select"
                    aria-label="Moneda del presupuesto"
                    value={currency}
                    onChange={(e) => setCurrency(e.target.value as '' | PublicCurrency)}
                  >
                    <option value="">Ambas</option>
                    <option value="USD">US$</option>
                    <option value="ARS">$</option>
                  </select>
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

      {/* Servicio Técnico Profesional — la tasación es la puerta de entrada del propietario,
          y es la única acción de la portada que no depende de que haya publicado algo. */}
      <section className="section" id="tasaciones">
        <div className="wrap">
          <div className="ctaband">
            <div className="ctaband-copy">
              <div className="kicker">
                <ChartIcon size={17} />
                Servicio Técnico Profesional
              </div>
              <h2>¿Querés saber cuánto vale tu propiedad?</h2>
              <p>
                Tasamos con comparables reales de la zona, valores de cierre efectivo y la oferta
                publicada al día de hoy. Te entregamos el informe por escrito, sin compromiso.
              </p>
              <ul className="claims">
                {TASACION_CLAIMS.map((claim) => (
                  <li key={claim}>
                    <CheckIcon size={16} />
                    {claim}
                  </li>
                ))}
              </ul>
            </div>
            <div className="ctaband-actions">
              <Link className="btn btn--primary btn--block" to={`/sitio/${slug}/contacto`}>
                <ChartIcon size={18} />
                Solicitar tasación
              </Link>
              <a
                className="btn btn--ghost btn--block"
                href={waTasacion(org.name)}
                target="_blank"
                rel="noopener noreferrer"
              >
                <ChatIcon />
                Coordinar por WhatsApp
              </a>
            </div>
          </div>
        </div>
      </section>

      {/* Identidad Corporativa — quién atiende del otro lado. El texto lo edita la
          inmobiliaria desde el panel (el mismo que se ve en /nosotros). */}
      <section className="section section--alt" id="empresa">
        <div className="wrap">
          <div className="identity">
            <div className="identity-copy">
              <div className="kicker">Identidad Corporativa</div>
              <h2>Detrás de cada operación hay un equipo que responde.</h2>
              <p>
                {org.site.aboutText
                  ?? `En ${org.name} acompañamos a propietarios e inquilinos en cada etapa: tasación, publicación, contrato y administración mensual. Los ajustes ICL e IPC se calculan solos y se avisan a tiempo, así nadie se entera del aumento tarde.`}
              </p>
              {org.address || org.phone || org.email ? (
                <div className="identity-data">
                  {org.address ? (
                    <div>
                      <div className="v"><PinIcon size={15} />{org.address}</div>
                      <div className="k">Oficinas comerciales. Atención personalizada con turno previo.</div>
                    </div>
                  ) : null}
                  {org.phone ? (
                    <div>
                      <div className="v"><PhoneIcon />{org.phone}</div>
                      <div className="k">Consultas por venta, alquiler y tasaciones.</div>
                    </div>
                  ) : null}
                  {org.email && !org.phone ? (
                    <div>
                      <div className="v"><MailIcon />{org.email}</div>
                      <div className="k">Te contestamos el mismo día hábil.</div>
                    </div>
                  ) : null}
                </div>
              ) : null}
              <Link className="link-btn" to={`/sitio/${slug}/nosotros`}>
                Conocer la empresa
                <ArrowRightIcon />
              </Link>
            </div>

            {/* Panel de marca, no una foto de la cartera: la segunda propiedad publicada puede
                ser una persiana con graffiti, y acá la imagen representa a la inmobiliaria.
                Con el logo cargado se ve el logo; sin él, el monograma de la inicial. */}
            <div className="identity-media">
              <div className="identity-brand">
                {org.hasLogo ? (
                  <img className="identity-logo" src={publicLogoUrl(slug)} alt={org.name} />
                ) : (
                  <div className="identity-mono" aria-hidden="true">{org.name.charAt(0).toUpperCase()}</div>
                )}
                <div className="n">{org.name}</div>
                <div className="s">Inmobiliaria</div>
              </div>
              <ul className="identity-pledges">
                {IDENTITY_PLEDGES.map((pledge) => (
                  <li key={pledge}>
                    <CheckIcon size={16} />
                    {pledge}
                  </li>
                ))}
              </ul>
            </div>
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
