import { lazy, Suspense } from 'react'

// Páginas del portal admin cargadas de forma perezosa (code-splitting por ruta). Viven en un
// módulo separado de `routes.tsx` a propósito: ese archivo también exporta `adminRoutes` (un
// array, no un componente), y react-refresh/only-export-components no tolera mezclar ambas cosas
// en el mismo archivo — ver routes.tsx.
export const DashboardPage = lazy(() => import('./pages/DashboardPage'))
export const ContratosPage = lazy(() => import('./pages/ContratosPage'))
export const ContratoDetailPage = lazy(() => import('./pages/ContratoDetailPage'))
export const IndexesPage = lazy(() => import('./pages/IndexesPage'))
export const PropiedadesPage = lazy(() => import('./pages/PropiedadesPage'))
export const InquilinosPage = lazy(() => import('./pages/InquilinosPage'))
export const PagosPage = lazy(() => import('./pages/PagosPage'))
export const ConsultasPage = lazy(() => import('./pages/ConsultasPage'))
export const AjustesPage = lazy(() => import('./pages/AjustesPage'))
export const DocumentosAdminPage = lazy(() => import('./pages/DocumentosAdminPage'))
export const ConfiguracionPage = lazy(() => import('./pages/ConfiguracionPage'))
export const MarcaPage = lazy(() => import('./pages/MarcaPage'))
export const RendicionesPage = lazy(() => import('./pages/RendicionesPage'))
export const CambiarClavePage = lazy(() => import('./pages/CambiarClavePage'))
export const NotFoundPage = lazy(() => import('./pages/NotFoundPage'))

function PageLoader() {
  return (
    <div style={{ padding: 40, textAlign: 'center', color: 'var(--muted)' }}>Cargando…</div>
  )
}

export function Lazy({ children }: { children: React.ReactNode }) {
  return <Suspense fallback={<PageLoader />}>{children}</Suspense>
}
