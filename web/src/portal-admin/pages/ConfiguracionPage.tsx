import { AdminTopbar } from '../layouts/AdminTopbar'
import { useAuthStore } from '@/shared/stores/authStore'

export default function ConfiguracionPage() {
  const user = useAuthStore((s) => s.user)

  return (
    <>
      <AdminTopbar crumbs={['Configuración']} />
      <div className="page">
        <div className="page-h">
          <div>
            <h1>Configuración</h1>
            <div className="lead">Datos de la organización</div>
          </div>
        </div>

        <div style={{ maxWidth: 560, display: 'flex', flexDirection: 'column', gap: 'var(--s-7)' }}>
          <div className="card">
            <div className="card-h">
              <h3>Organización</h3>
            </div>
            <div className="card-b" style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
              <Row label="ID" value={user?.organizationId ?? '—'} mono />
              <Row label="Slug" value={user?.organizationSlug ?? '—'} />
              <Row label="Usuario" value={user?.email ?? '—'} />
              <Row label="Rol" value={user?.role ?? '—'} />
            </div>
          </div>

          <div className="card">
            <div className="card-h">
              <h3>Almacenamiento</h3>
              <div className="sub">configuración activa</div>
            </div>
            <div className="card-b" style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
              <Row label="Motor" value="Almacenamiento local" />
              <Row label="Token de documentos" value="HMAC-SHA256 · vence en 5 min" />
              <Row label="Tamaño máximo por archivo" value="50 MB" />
            </div>
          </div>

          <div className="card">
            <div className="card-h">
              <h3>Índices de ajuste</h3>
              <div className="sub">fuentes de datos</div>
            </div>
            <div className="card-b" style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
              <Row label="ICL" value="API BCRA · api.bcra.gob.ar" />
              <Row label="IPC" value="INDEC vía datos.gob.ar" />
              <Row label="Reintentos" value="3 intentos con backoff de 2 s" />
            </div>
          </div>
        </div>
      </div>
    </>
  )
}

function Row({ label, value, mono = false }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="between">
      <span style={{ fontSize: 'var(--fs-sm)', color: 'var(--muted)' }}>{label}</span>
      <span style={{ fontSize: 'var(--fs-sm)', fontWeight: 500, fontFamily: mono ? 'monospace' : undefined }}>
        {value}
      </span>
    </div>
  )
}
