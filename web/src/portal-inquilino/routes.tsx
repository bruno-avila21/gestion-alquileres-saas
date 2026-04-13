import type { RouteObject } from 'react-router'
import InquilinoLayout from './layouts/InquilinoLayout'
import TenantLoginPage from './pages/LoginPage'

function InquilinoHome() {
  return <div className="p-6"><h1 className="text-2xl font-semibold">Portal del inquilino</h1><p className="text-slate-600">Contenido disponible en fases futuras.</p></div>
}

export const inquilinoRoutes: RouteObject[] = [
  { path: 'login', element: <TenantLoginPage /> },
  {
    path: '',
    element: <InquilinoLayout />,
    children: [{ index: true, element: <InquilinoHome /> }],
  },
]
