import { useState } from 'react'
import { NavLink, useNavigate } from 'react-router'
import { useAuthStore } from '@/shared/stores/authStore'
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog'
import { IcCash, IcDoc, IcHome, IcLogout, IcShield } from '@/shared/components/ui/Icons'

const ITEMS = [
  { to: '/inquilino', label: 'Inicio', Icon: IcHome, end: true },
  { to: '/inquilino/contrato', label: 'Contrato', Icon: IcDoc, end: false },
  { to: '/inquilino/documentos', label: 'Docs', Icon: IcShield, end: false },
  { to: '/inquilino/pagos', label: 'Pagos', Icon: IcCash, end: false },
] as const

// 48px de alto y 64px de ancho mínimos: por debajo de eso el objetivo táctil queda fuera
// de lo que se puede tocar con confianza caminando (WCAG 2.2 AA pide 24px como piso duro).
const itemStyle: React.CSSProperties = {
  display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center',
  gap: 4, minWidth: 64, minHeight: 48, padding: '4px 8px',
  background: 'none', border: 'none', cursor: 'pointer',
  fontFamily: 'inherit', textDecoration: 'none',
  borderTop: '2px solid transparent',
}

const labelStyle: React.CSSProperties = { fontSize: 10 }

/**
 * Navegación inferior del portal de inquilinos. Se monta UNA sola vez en InquilinoLayout.
 *
 * Antes estaba copiada en las cuatro páginas, con implementaciones que ya habían divergido entre
 * sí, y no existía forma de cerrar sesión en todo el portal: en un teléfono prestado o compartido
 * la sesión quedaba abierta sobre datos financieros propios.
 *
 * El estado activo no se comunica sólo por color (invisible para daltonismo): lleva además barra
 * superior, peso tipográfico y `aria-current="page"`.
 */
export function TenantBottomNav() {
  const navigate = useNavigate()
  const logout = useAuthStore((s) => s.logout)
  const [confirmLogout, setConfirmLogout] = useState(false)

  return (
    <>
      <nav
        aria-label="Navegación principal"
        style={{
          position: 'fixed', bottom: 0, left: 0, right: 0,
          background: 'var(--surface)', borderTop: '1px solid var(--hairline)',
          display: 'flex', justifyContent: 'space-around',
          padding: '4px 0 max(12px, env(safe-area-inset-bottom))', zIndex: 100,
        }}
      >
        {ITEMS.map(({ to, label, Icon, end }) => (
          <NavLink
            key={to}
            to={to}
            end={end}
            style={({ isActive }) => ({
              ...itemStyle,
              color: isActive ? 'var(--brand)' : 'var(--muted)',
              borderTopColor: isActive ? 'var(--brand)' : 'transparent',
            })}
          >
            {({ isActive }) => (
              <>
                <Icon size={20} />
                <span style={{ ...labelStyle, fontWeight: isActive ? 700 : 500 }}>{label}</span>
              </>
            )}
          </NavLink>
        ))}

        <button type="button" onClick={() => setConfirmLogout(true)} style={{ ...itemStyle, color: 'var(--muted)' }}>
          <IcLogout size={20} />
          <span style={{ ...labelStyle, fontWeight: 500 }}>Salir</span>
        </button>
      </nav>

      <ConfirmDialog
        open={confirmLogout}
        title="¿Cerrar sesión?"
        description="Vas a tener que volver a ingresar con tu email y contraseña para ver tu contrato y tus pagos."
        confirmLabel="Cerrar sesión"
        onConfirm={() => {
          setConfirmLogout(false)
          logout()
          navigate('/inquilino/login', { replace: true })
        }}
        onCancel={() => setConfirmLogout(false)}
      />
    </>
  )
}
