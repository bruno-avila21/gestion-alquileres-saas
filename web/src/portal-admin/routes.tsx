import type { RouteObject } from 'react-router'
import AdminLayout from './layouts/AdminLayout'
import AdminLoginPage from './pages/LoginPage'
import RegisterOrgPage from './pages/RegisterOrgPage'
import DashboardPage from './pages/DashboardPage'

export const adminRoutes: RouteObject[] = [
  { path: 'login', element: <AdminLoginPage /> },
  { path: 'register-org', element: <RegisterOrgPage /> },
  {
    path: '',
    element: <AdminLayout />,
    children: [
      { path: 'dashboard', element: <DashboardPage /> },
      { index: true, element: <DashboardPage /> },
    ],
  },
]
