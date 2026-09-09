import { Link, Outlet, useLocation, useParams, useSearchParams } from 'react-router'
import { usePublicOrg } from '@/features/public/hooks/usePublic'
import '../publico.css'
import { usePpTheme } from '../hooks/usePpTheme'
import { WhatsAppFloat } from '../components/WhatsAppFloat'
import { BurgerIcon, ThemeIcon } from '../components/icons'
import { PublicoNotFound } from '../pages/PublicoNotFound'

function NavItem({ to, active, children }: { to: string; active: boolean; children: React.ReactNode }) {
  return (
    <Link to={to} className={active ? 'active' : undefined}>
      {children}
    </Link>
  )
}

export default function PublicoLayout() {
  const { slug } = useParams<{ slug: string }>()
  const { data: org, isLoading, isError } = usePublicOrg(slug)
  const { theme, toggleTheme } = usePpTheme()
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

  return (
    <div className="pp-app" data-theme={theme ?? undefined}>
      <a href="#pp-main" className="visually-hidden">Saltar al contenido</a>
      <header className="topbar">
        <div className="wrap">
          <Link className="brand" to={base}>
            <div className="brand-mark" aria-hidden="true">{org.name.charAt(0).toUpperCase()}</div>
            <div className="brand-name">{org.name}</div>
          </Link>
          <nav className="nav" aria-label="Principal">
            <NavItem to={`${base}/propiedades?operation=Sale`} active={isPropiedades && operation === 'Sale'}>Venta</NavItem>
            <NavItem to={`${base}/propiedades?operation=Rent`} active={isPropiedades && operation === 'Rent'}>Alquiler</NavItem>
            <NavItem to={`${base}/propiedades`} active={isPropiedades && !operation}>Propiedades</NavItem>
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
                <div className="brand-mark" aria-hidden="true">{org.name.charAt(0).toUpperCase()}</div>
                <div className="brand-name">{org.name}</div>
              </div>
              <p>Venta, alquiler y tasaciones con acompañamiento de principio a fin.</p>
            </div>
            <div>
              <h4>Operaciones</h4>
              <Link to={`${base}/propiedades?operation=Sale`}>Venta</Link>
              <Link to={`${base}/propiedades?operation=Rent`}>Alquiler</Link>
            </div>
            <div>
              <h4>Inmobiliaria</h4>
              <Link to={`${base}/nosotros`}>La Empresa</Link>
              <Link to={`${base}/contacto`}>Contacto</Link>
            </div>
            <div>
              <h4>Contacto</h4>
              <Link to={`${base}/contacto`}>Escribinos</Link>
            </div>
          </div>
          <div className="foot-bottom">
            <span>© {new Date().getFullYear()} {org.name}. Todos los derechos reservados.</span>
          </div>
        </div>
      </footer>

      <WhatsAppFloat orgName={org.name} />
    </div>
  )
}
