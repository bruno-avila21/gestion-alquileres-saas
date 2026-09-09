import type { RouteObject } from 'react-router'
import AdminLayout from './layouts/AdminLayout'
import AdminLoginPage from './pages/LoginPage'
import RegisterOrgPage from './pages/RegisterOrgPage'
import {
  Lazy,
  DashboardPage,
  ContratosPage,
  ContratoDetailPage,
  IndexesPage,
  PropiedadesPage,
  InquilinosPage,
  PagosPage,
  ConsultasPage,
  AjustesPage,
  DocumentosAdminPage,
  ConfiguracionPage,
  MarcaPage,
  RendicionesPage,
  CambiarClavePage,
  NotFoundPage,
} from './routes.lazy'

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
      { path: 'rendiciones', element: <Lazy><RendicionesPage /></Lazy> },
      { path: 'consultas', element: <Lazy><ConsultasPage /></Lazy> },
      { path: 'ajustes', element: <Lazy><AjustesPage /></Lazy> },
      { path: 'documentos', element: <Lazy><DocumentosAdminPage /></Lazy> },
      { path: 'configuracion', element: <Lazy><ConfiguracionPage /></Lazy> },
      { path: 'configuracion/marca', element: <Lazy><MarcaPage /></Lazy> },
      { path: 'cambiar-clave', element: <Lazy><CambiarClavePage /></Lazy> },
      { path: '*', element: <Lazy><NotFoundPage /></Lazy> },
    ],
  },
]
