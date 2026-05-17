import type { RouteObject } from 'react-router'
import AdminLayout from './layouts/AdminLayout'
import AdminLoginPage from './pages/LoginPage'
import RegisterOrgPage from './pages/RegisterOrgPage'
import DashboardPage from './pages/DashboardPage'
import ContratosPage from './pages/ContratosPage'
import ContratoDetailPage from './pages/ContratoDetailPage'
import IndexesPage from './pages/IndexesPage'

export const adminRoutes: RouteObject[] = [
  { path: 'login', element: <AdminLoginPage /> },
  { path: 'register-org', element: <RegisterOrgPage /> },
  {
    path: '',
    element: <AdminLayout />,
    children: [
      { index: true, element: <DashboardPage /> },
      { path: 'dashboard', element: <DashboardPage /> },
      { path: 'contratos', element: <ContratosPage /> },
      { path: 'contratos/:id', element: <ContratoDetailPage /> },
      { path: 'indices', element: <IndexesPage /> },
    ],
  },
]
