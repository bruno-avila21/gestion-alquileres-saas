import { createBrowserRouter, Navigate, RouterProvider } from 'react-router'
import { QueryClientProvider } from '@tanstack/react-query'
import { queryClient } from '@/shared/lib/queryClient'
import { adminRoutes } from '@/portal-admin/routes'
import { inquilinoRoutes } from '@/portal-inquilino/routes'

const router = createBrowserRouter([
  { path: '/admin/*', children: adminRoutes },
  { path: '/inquilino/*', children: inquilinoRoutes },
  { path: '/', element: <Navigate to="/admin/login" replace /> },
  { path: '*', element: <Navigate to="/admin/login" replace /> },
])

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  )
}
