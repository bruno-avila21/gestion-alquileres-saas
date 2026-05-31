import { lazy, Suspense } from 'react'
import { createBrowserRouter, Navigate, RouterProvider } from 'react-router'
import { QueryClientProvider } from '@tanstack/react-query'
import { queryClient } from '@/shared/lib/queryClient'
import { adminRoutes } from '@/portal-admin/routes'
import { inquilinoRoutes } from '@/portal-inquilino/routes'

const NotFoundPage = lazy(() => import('@/portal-admin/pages/NotFoundPage'))

const router = createBrowserRouter([
  { path: '/admin/*', children: adminRoutes },
  { path: '/inquilino/*', children: inquilinoRoutes },
  { path: '/', element: <Navigate to="/admin/login" replace /> },
  { path: '*', element: <Suspense fallback={null}><NotFoundPage /></Suspense> },
])

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  )
}
