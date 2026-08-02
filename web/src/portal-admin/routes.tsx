import { lazy, Suspense } from 'react'
import type { RouteObject } from 'react-router'
import AdminLayout from './layouts/AdminLayout'
import AdminLoginPage from './pages/LoginPage'
import RegisterOrgPage from './pages/RegisterOrgPage'

const DashboardPage = lazy(() => import('./pages/DashboardPage'))
const ContratosPage = lazy(() => import('./pages/ContratosPage'))
const ContratoDetailPage = lazy(() => import('./pages/ContratoDetailPage'))
const IndexesPage = lazy(() => import('./pages/IndexesPage'))
const PropiedadesPage = lazy(() => import('./pages/PropiedadesPage'))
const InquilinosPage = lazy(() => import('./pages/InquilinosPage'))
const PagosPage = lazy(() => import('./pages/PagosPage'))
const AjustesPage = lazy(() => import('./pages/AjustesPage'))
const DocumentosAdminPage = lazy(() => import('./pages/DocumentosAdminPage'))
const ConfiguracionPage = lazy(() => import('./pages/ConfiguracionPage'))
const CambiarClavePage = lazy(() => import('./pages/CambiarClavePage'))
const NotFoundPage = lazy(() => import('./pages/NotFoundPage'))

function PageLoader() {
  return (
    <div style={{ padding: 40, textAlign: 'center', color: 'var(--muted)' }}>Cargando…</div>
  )
}

function Lazy({ children }: { children: React.ReactNode }) {
  return <Suspense fallback={<PageLoader />}>{children}</Suspense>
}

export const adminRoutes: RouteObject[] = [
  { path: 'login', element: <AdminLoginPage /> },
  { path: 'register-org', element: <RegisterOrgPage /> },
  {
    path: '',
    element: <AdminLayout />,
    children: [
      { index: true, element: <Lazy><DashboardPage /></Lazy> },
      { path: 'dashboard', element: <Lazy><DashboardPage /></Lazy> },
      { path: 'contratos', element: <Lazy><ContratosPage /></Lazy> },
      { path: 'contratos/:id', element: <Lazy><ContratoDetailPage /></Lazy> },
      { path: 'indices', element: <Lazy><IndexesPage /></Lazy> },
      { path: 'propiedades', element: <Lazy><PropiedadesPage /></Lazy> },
      { path: 'inquilinos', element: <Lazy><InquilinosPage /></Lazy> },
      { path: 'pagos', element: <Lazy><PagosPage /></Lazy> },
      { path: 'ajustes', element: <Lazy><AjustesPage /></Lazy> },
      { path: 'documentos', element: <Lazy><DocumentosAdminPage /></Lazy> },
      { path: 'configuracion', element: <Lazy><ConfiguracionPage /></Lazy> },
      { path: 'cambiar-clave', element: <Lazy><CambiarClavePage /></Lazy> },
      { path: '*', element: <Lazy><NotFoundPage /></Lazy> },
    ],
  },
]
