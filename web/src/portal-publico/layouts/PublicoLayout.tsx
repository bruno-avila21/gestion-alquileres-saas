import { Link, Outlet, useLocation, useParams, useSearchParams } from 'react-router'
import { usePublicOrg } from '@/features/public/hooks/usePublic'
import '../publico.css'
import { usePpTheme } from '../hooks/usePpTheme'
import { useSiteFonts } from '../hooks/useSiteFonts'
import { accentVars, FONT_PAIRINGS } from '@/features/public/utils/siteTheme'
import { publicLogoUrl } from '@/features/public/utils/resolvePublicPhotoUrl'
import { WhatsAppFloat } from '../components/WhatsAppFloat'
import { BurgerIcon, MailIcon, PhoneIcon, PinIcon, ThemeIcon } from '../components/icons'
import { PublicoNotFound } from '../pages/PublicoNotFound'

function NavItem({ to, active, children }: { to: string; active: boolean; children: React.ReactNode }) {
  return (
    <Link to={to} className={active ? 'active' : undefined}>
      {children}
    </Link>
  )
}

/**
 * La marca: el logo que cargó la inmobiliaria, o el monograma con su inicial mientras no
 * haya cargado ninguno. El mismo bloque va en el encabezado y en el pie.
 */
function Marca({ org, slug }: { org: { name: string; hasLogo: boolean }; slug: string }) {
  return (
    <>
      {org.hasLogo ? (
        <img className="brand-logo" src={publicLogoUrl(slug)} alt={org.name} />
      ) : (
        <div className="brand-mark" aria-hidden="true">{org.name.charAt(0).toUpperCase()}</div>
      )}
      <div className="brand-name">
        {org.name}
        <small>Inmobiliaria</small>
      </div>
    </>
  )
}

export default function PublicoLayout() {
  const { slug } = useParams<{ slug: string }>()
  const { data: org, isLoading, isError } = usePublicOrg(slug)
  const { theme, toggleTheme } = usePpTheme()
  useSiteFonts(org?.site.fontPairing)
  const { pathname } = useLocation()
  const [searchParams] = useSearchParams()

  const base = `/sitio/${slug}`
  const isPropiedades = pathname.startsWith(`${base}/propiedades`)
  const operation = searchParams.get('operation')

  if (isLoading) {
    return (
      <div className="pp-app" data-theme={theme ?? undefined}>
        <div className="state-box" role="status">
          <div className="spinner" />
          Cargando sitio…
        </div>
      </div>
    )
  }

  if (isError || !org) {
    return (
      <div className="pp-app" data-theme={theme ?? undefined}>
        <PublicoNotFound />
      </div>
    )
  }

  const telHref = org.phone ? `tel:${org.phone.replace(/[^\d+]/g, '')}` : null

  // El acento y las familias tipográficas se pisan como variables en el nodo raíz del sitio,
  // no en <html>: el panel y el portal de inquilinos comparten documento y tienen su propio
  // color, así que teñir la raíz les cambiaría el aspecto a ellos también.
  const fonts = FONT_PAIRINGS[org.site.fontPairing ?? 'jakarta']
  const themeVars = {
    ...accentVars(org.site.accentColor, theme),
    '--font-headings': fonts.headings,
    '--font-body': fonts.body,
  } as React.CSSProperties

  return (
    <div className="pp-app" data-theme={theme ?? undefined} style={themeVars}>
      <a href="#pp-main" className="visually-hidden">Saltar al contenido</a>

      <header className="topbar">
        <div className="wrap">
          <Link className="brand" to={base}>
            <Marca org={org} slug={slug as string} />
          </Link>

          <nav className="nav" aria-label="Principal">
            <NavItem to={`${base}/propiedades`} active={isPropiedades && !operation}>Propiedades</NavItem>
            <NavItem to={`${base}/propiedades?operation=Sale`} active={isPropiedades && operation === 'Sale'}>Venta</NavItem>
            <NavItem to={`${base}/propiedades?operation=Rent`} active={isPropiedades && operation === 'Rent'}>Alquiler</NavItem>
            <NavItem to={`${base}/nosotros`} active={pathname === `${base}/nosotros`}>La Empresa</NavItem>
            <NavItem to={`${base}/contacto`} active={pathname === `${base}/contacto`}>Contacto</NavItem>
          </nav>

          <div className="top-actions">
            <button
              className="icon-btn"
              type="button"
              title="Cambiar tema"
              aria-label="Cambiar tema"
              onClick={toggleTheme}
            >
              <ThemeIcon />
            </button>
            <Link className="icon-btn burger" to={`${base}/propiedades`} aria-label="Ver propiedades">
              <BurgerIcon />
            </Link>
            <Link className="top-cta" to={`${base}/contacto`}>Tasá tu propiedad</Link>
          </div>
        </div>
      </header>

      <main id="pp-main" style={{ flex: 1 }}>
        <Outlet context={{ org, slug: slug as string }} />
      </main>

      <footer className="foot">
        <div className="wrap">
          <div className="cols">
            <div className="about">
              <div className="brand" style={{ margin: 0 }}>
                <Marca org={org} slug={slug as string} />
              </div>
              <p>
                {org.site.footerTagline
                  ?? 'Venta, alquiler y tasaciones con acompañamiento de principio a fin. Contratos administrados con ajustes ICL e IPC calculados y notificados a tiempo.'}
              </p>
            </div>

            <div>
              <h4>Operaciones</h4>
              <Link to={`${base}/propiedades?operation=Sale`}>Venta</Link>
              <Link to={`${base}/propiedades?operation=Rent`}>Alquiler</Link>
              <Link to={`${base}/propiedades`}>Todas las propiedades</Link>
            </div>

            <div>
              <h4>Inmobiliaria</h4>
              <Link to={`${base}/nosotros`}>La Empresa</Link>
              <Link to={`${base}/contacto`}>Tasaciones</Link>
              <Link to={`${base}/contacto`}>Contacto</Link>
            </div>

            <div>
              <h4>Contacto</h4>
              {org.address ? (
                <span className="contact-line"><PinIcon size={15} />{org.address}</span>
              ) : null}
              {telHref ? (
                <a href={telHref} className="contact-line"><PhoneIcon />{org.phone}</a>
              ) : null}
              {org.email ? (
                <a href={`mailto:${org.email}`} className="contact-line"><MailIcon />{org.email}</a>
              ) : null}
              {!org.address && !org.phone && !org.email ? (
                <Link to={`${base}/contacto`}>Escribinos</Link>
              ) : null}
            </div>
          </div>

          <div className="foot-bottom">
            <span>© {new Date().getFullYear()} {org.name}. Todos los derechos reservados.</span>
            <span>Ajustes ICL/IPC automatizados</span>
          </div>

          <p className="foot-legal">
            Las medidas enunciadas son orientativas; las exactas son las del respectivo título de
            propiedad. Fotos, imágenes y videos son ilustrativos y no contractuales. Los precios
            publicados son orientativos y no contractuales.
          </p>
        </div>
      </footer>

      <WhatsAppFloat orgName={org.name} />
    </div>
  )
}
