/* eslint-disable react-refresh/only-export-components --
   Este módulo define la tabla de rutas (`RouteObject[]`), no un componente: no participa del
   límite de Fast Refresh y la regla dispara un falso positivo sobre los `lazy(...)` locales
   (mismo patrón ya presente, sin suprimir, en portal-admin/routes.tsx). */
import { lazy } from 'react'
import type { RouteObject } from 'react-router'
import PublicoLayout from './layouts/PublicoLayout'
import { Lazy } from './components/RouteLoader'

const HomePage = lazy(() => import('./pages/HomePage'))
const ListadoPage = lazy(() => import('./pages/ListadoPage'))
const FichaPage = lazy(() => import('./pages/FichaPage'))
const ContactoPage = lazy(() => import('./pages/ContactoPage'))
const NosotrosPage = lazy(() => import('./pages/NosotrosPage'))

export const publicoRoutes: RouteObject[] = [
  {
    path: '',
    element: <PublicoLayout />,
    children: [
      { index: true, element: <Lazy><HomePage /></Lazy> },
      { path: 'propiedades', element: <Lazy><ListadoPage /></Lazy> },
      { path: 'propiedades/:id', element: <Lazy><FichaPage /></Lazy> },
      { path: 'contacto', element: <Lazy><ContactoPage /></Lazy> },
      { path: 'nosotros', element: <Lazy><NosotrosPage /></Lazy> },
    ],
  },
]
