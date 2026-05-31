import { useNavigate } from 'react-router'

export default function NotFoundPage() {
  const navigate = useNavigate()
  return (
    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100vh', gap: 16, color: 'var(--muted)', textAlign: 'center', padding: '0 24px' }}>
      <div style={{ fontSize: 72, fontWeight: 800, color: 'var(--brand)', opacity: 0.25, lineHeight: 1 }}>404</div>
      <h1 style={{ fontSize: 20, fontWeight: 600, color: 'var(--fg)', margin: 0 }}>Página no encontrada</h1>
      <p style={{ margin: 0 }}>La URL que ingresaste no existe en esta aplicación.</p>
      <button className="btn" onClick={() => navigate('/admin/dashboard')}>
        Ir al inicio
      </button>
    </div>
  )
}
