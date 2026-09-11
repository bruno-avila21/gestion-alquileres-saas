import { Suspense } from 'react'

function PageLoader() {
  return (
    <div className="state-box" role="status">
      <div className="spinner" />
      Cargando…
    </div>
  )
}

/** Envuelve una página lazy-loaded del sitio público con un fallback de carga consistente. */
export function Lazy({ children }: { children: React.ReactNode }) {
  return <Suspense fallback={<PageLoader />}>{children}</Suspense>
}
