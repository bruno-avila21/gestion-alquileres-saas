import type { RouteObject } from 'react-router'
import InquilinoLayout from './layouts/InquilinoLayout'
import TenantLoginPage from './pages/LoginPage'
import TenantHomePage from './pages/HomePage'
import TenantContratoPage from './pages/ContratoPage'
import TenantDocumentosPage from './pages/DocumentosPage'
import TenantPagosPage from './pages/PagosPage'

export const inquilinoRoutes: RouteObject[] = [
  { path: 'login', element: <TenantLoginPage /> },
  {
    path: '',
    element: <InquilinoLayout />,
    children: [
      { index: true, element: <TenantHomePage /> },
      { path: 'contrato', element: <TenantContratoPage /> },
      { path: 'documentos', element: <TenantDocumentosPage /> },
      { path: 'pagos', element: <TenantPagosPage /> },
    ],
  },
]
