import { useNavigate } from 'react-router'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { useAuthStore } from '@/shared/stores/authStore'

export default function NotFoundPage() {
  const navigate = useNavigate()
  const user = useAuthStore((s) => s.user)
  // This 404 is the global fallback, so a tenant can land here too — route each role to its own home
  // instead of always bouncing to the admin portal (audit B15).
  const home = user?.role === 'Tenant' ? '/inquilino' : '/admin/dashboard'

  return (
    <>
      <AdminTopbar crumbs={['No encontrada']} />
      {/* Ocupa lo que le deja el shell, no `100vh`: el shell ya mide la ventana entera y
          anidar otra altura completa acá metía scroll de más en una pantalla sin contenido. */}
      <div
        className="page"
        style={{
          flex: 1, display: 'flex', flexDirection: 'column',
          alignItems: 'center', justifyContent: 'center',
          gap: 16, textAlign: 'center', color: 'var(--muted)',
        }}
      >
        <div style={{ fontSize: 72, fontWeight: 800, color: 'var(--brand)', opacity: 0.25, lineHeight: 1 }}>404</div>
        <h1 style={{ fontSize: 20, fontWeight: 600, color: 'var(--ink)', margin: 0 }}>Página no encontrada</h1>
        <p style={{ margin: 0 }}>La URL que ingresaste no existe en esta aplicación.</p>
        <button className="btn" onClick={() => navigate(home)}>
          Ir al inicio
        </button>
      </div>
    </>
  )
}
