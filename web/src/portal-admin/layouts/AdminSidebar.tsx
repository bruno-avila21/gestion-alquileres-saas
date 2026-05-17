import { NavLink, useNavigate } from 'react-router'
import { useAuthStore } from '@/shared/stores/authStore'
import {
  IcHome, IcDoc, IcBuilding, IcUsers, IcCash,
  IcTrend, IcShield, IcSettings, IcLogout, IcCalendar,
} from '@/shared/components/ui/Icons'

const NAV_ITEMS = [
  { to: '/admin', label: 'Panel', icon: <IcHome size={18} />, end: true },
  { to: '/admin/contratos', label: 'Contratos', icon: <IcDoc size={18} /> },
  { to: '/admin/propiedades', label: 'Propiedades', icon: <IcBuilding size={18} /> },
  { to: '/admin/propietarios', label: 'Propietarios', icon: <IcUsers size={18} /> },
  { to: '/admin/inquilinos', label: 'Inquilinos', icon: <IcUsers size={18} /> },
  { to: '/admin/pagos', label: 'Pagos', icon: <IcCash size={18} /> },
  { to: '/admin/ajustes', label: 'Ajustes', icon: <IcCalendar size={18} /> },
  { to: '/admin/indices', label: 'Índices', icon: <IcTrend size={18} /> },
  { to: '/admin/documentos', label: 'Documentos', icon: <IcShield size={18} /> },
  { to: '/admin/configuracion', label: 'Configuración', icon: <IcSettings size={18} /> },
]

export function AdminSidebar() {
  const user = useAuthStore((s) => s.user)
  const logout = useAuthStore((s) => s.logout)
  const navigate = useNavigate()

  function handleLogout() {
    logout()
    navigate('/admin/login', { replace: true })
  }

  const initials = user?.email
    ? user.email.slice(0, 2).toUpperCase()
    : 'A'

  return (
    <aside className="sidebar">
      <div className="brand">
        <div className="lg">A</div>
        <div>
          <div className="nm">Alquilar.io</div>
          <div className="org">{user?.organizationSlug ?? 'admin'}</div>
        </div>
      </div>

      <nav>
        {NAV_ITEMS.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.end}
            className={({ isActive }) => isActive ? 'active' : ''}
          >
            {item.icon}
            {item.label}
          </NavLink>
        ))}
      </nav>

      <div className="footer">
        <div
          className="mono-avatar"
          style={{ background: 'var(--brand-50)', color: 'var(--brand-700)', borderColor: 'var(--brand-100)' }}
        >
          {initials}
        </div>
        <div style={{ flex: 1, minWidth: 0 }}>
          <div style={{ fontSize: 'var(--fs-xs)', fontWeight: 500, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
            {user?.email ?? ''}
          </div>
        </div>
        <button
          className="btn btn--ghost btn--icon btn--sm"
          onClick={handleLogout}
          title="Cerrar sesión"
        >
          <IcLogout size={14} />
        </button>
      </div>
    </aside>
  )
}
