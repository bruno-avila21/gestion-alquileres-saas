import { useNavigate } from 'react-router'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { ChangePasswordForm } from '@/features/auth/components/ChangePasswordForm'
import { useAuthStore } from '@/shared/stores/authStore'

export default function AdminCambiarClavePage() {
  const navigate = useNavigate()
  const forced = useAuthStore((s) => s.user?.mustChangePassword ?? false)

  return (
    <>
      <AdminTopbar crumbs={['Seguridad', forced ? 'Elegí tu contraseña' : 'Cambiar contraseña']} />
      {/* Mismo esqueleto que el resto del panel (`page` + `page-h` + h1 + `lead`). Antes esta
          pantalla se armaba con estilos propios y quedaba sin miga de pan y con otro margen
          que las demás, justo cuando es obligatoria en el primer ingreso. */}
      <div className="page">
        <div className="page-h">
          <div>
            <h1>{forced ? 'Elegí tu contraseña' : 'Cambiar contraseña'}</h1>
            <div className="lead">Se aplica a tu usuario, no al de la organización.</div>
          </div>
        </div>

        <div style={{ maxWidth: 460 }}>
          <ChangePasswordForm forced={forced} onDone={() => navigate('/admin', { replace: true })} />
        </div>
      </div>
    </>
  )
}
