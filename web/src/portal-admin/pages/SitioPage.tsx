import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { AdminTopbar } from '../layouts/AdminTopbar'
import { IcCheck, IcArrowUp } from '@/shared/components/ui/Icons'
import { useAuthStore } from '@/shared/stores/authStore'
import { useSiteSettings, useUpdateSiteSettings } from '@/features/site/hooks/useSiteSettings'
import {
  DEFAULT_ACCENT, FONT_PAIRINGS, SITE_FONT_PAIRINGS, inkOn,
  type SiteFontPairing,
} from '@/features/public/utils/siteTheme'

/** Los mismos textos que el sitio usa cuando el campo está vacío, para mostrarlos de placeholder. */
const PLACEHOLDERS = {
  heroTitle: 'Encontrá tu próxima propiedad con quien conoce la zona.',
  heroSubtitle: 'Venta y alquiler con fichas claras, precios al día y contacto directo.',
  aboutText: 'Acompañamos a propietarios e inquilinos en cada etapa de la operación: venta, alquiler y tasaciones…',
  footerTagline: 'Venta, alquiler y tasaciones con acompañamiento de principio a fin.',
}

export default function SitioPage() {
  const slug = useAuthStore((s) => s.user?.organizationSlug)
  const { data, isLoading } = useSiteSettings()
  const update = useUpdateSiteSettings()

  const [accent, setAccent] = useState(DEFAULT_ACCENT)
  const [font, setFont] = useState<SiteFontPairing>('jakarta')
  const [heroTitle, setHeroTitle] = useState('')
  const [heroSubtitle, setHeroSubtitle] = useState('')
  const [aboutText, setAboutText] = useState('')
  const [footerTagline, setFooterTagline] = useState('')

  // El formulario se siembra una sola vez, cuando llega la configuración. Si se sincronizara
  // en cada render, cada tecla que escribe el usuario se pisaría con lo que hay en la caché.
  useEffect(() => {
    if (!data) return
    setAccent(data.accentColor ?? DEFAULT_ACCENT)
    setFont(data.fontPairing ?? 'jakarta')
    setHeroTitle(data.heroTitle ?? '')
    setHeroSubtitle(data.heroSubtitle ?? '')
    setAboutText(data.aboutText ?? '')
    setFooterTagline(data.footerTagline ?? '')
  }, [data])

  function handleGuardar() {
    update.mutate({
      // El acento en el valor por defecto se guarda como null: así una inmobiliaria que no
      // eligió color se lleva el violeta nuevo si algún día cambia el diseño, en vez de
      // quedar clavada en el de hoy.
      accentColor: accent.toLowerCase() === DEFAULT_ACCENT ? null : accent,
      fontPairing: font === 'jakarta' ? null : font,
      heroTitle: heroTitle.trim() || null,
      heroSubtitle: heroSubtitle.trim() || null,
      aboutText: aboutText.trim() || null,
      footerTagline: footerTagline.trim() || null,
    })
  }

  if (isLoading) {
    return (
      <>
        <AdminTopbar crumbs={['Configuración', 'Sitio público']} />
        <div className="page"><div style={{ color: 'var(--muted)' }}>Cargando…</div></div>
      </>
    )
  }

  return (
    <>
      <AdminTopbar
        crumbs={['Configuración', 'Sitio público']}
        right={
          <button className="btn btn--sm btn--primary" onClick={handleGuardar} disabled={update.isPending}>
            {update.isPending ? 'Guardando…' : 'Guardar cambios'}
          </button>
        }
      />
      <div className="page">
        <div className="page-h">
          <div>
            <h1>Sitio público</h1>
            <div className="lead">Cómo se ve y qué dice tu sitio. Lo que dejes vacío usa el diseño por defecto.</div>
          </div>
          {slug ? (
            <a className="btn btn--sm" href={`/sitio/${slug}`} target="_blank" rel="noopener noreferrer">
              Ver mi sitio <IcArrowUp size={12} />
            </a>
          ) : null}
        </div>

        <div style={{ maxWidth: 760, display: 'flex', flexDirection: 'column', gap: 'var(--s-7)' }}>
          <div className="card">
            <div className="card-h"><h3>Color</h3></div>
            <div className="card-b" style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
              <div className="row" style={{ gap: 12, alignItems: 'center' }}>
                <input
                  type="color"
                  aria-label="Color de acción del sitio"
                  value={accent}
                  onChange={(e) => setAccent(e.target.value)}
                  style={{ width: 46, height: 38, padding: 2, border: '1px solid var(--hairline-2)', borderRadius: 'var(--r-3)', background: 'var(--surface)' }}
                />
                <input
                  className="input"
                  style={{ width: 130, fontFamily: 'var(--font-mono, monospace)' }}
                  value={accent}
                  onChange={(e) => setAccent(e.target.value)}
                  aria-label="Color en hexadecimal"
                />
                <button className="btn btn--sm" onClick={() => setAccent(DEFAULT_ACCENT)}>
                  Volver al de fábrica
                </button>
              </div>

              {/* Vista previa con el color aplicado de verdad, y con la tinta que el sitio va a
                  calcular encima. Si elegís un amarillo, acá se ve que el texto pasa a oscuro
                  en lugar de quedar un botón blanco sobre blanco. */}
              <div className="row" style={{ gap: 10, alignItems: 'center' }}>
                <span
                  style={{
                    display: 'inline-flex', alignItems: 'center', gap: 8, height: 40, padding: '0 18px',
                    borderRadius: 8, background: accent, color: inkOn(accent), fontWeight: 700, fontSize: 14,
                  }}
                >
                  Consultar
                </span>
                <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>
                  Así va a verse el botón principal del sitio. El color del texto se calcula
                  para que se lea sobre el fondo que elijas.
                </span>
              </div>
            </div>
          </div>

          <div className="card">
            <div className="card-h"><h3>Tipografía</h3></div>
            <div className="card-b" style={{ display: 'grid', gap: 10 }}>
              {SITE_FONT_PAIRINGS.map((f) => {
                const spec = FONT_PAIRINGS[f]
                const activa = font === f
                return (
                  <button
                    key={f}
                    type="button"
                    onClick={() => setFont(f)}
                    style={{
                      display: 'flex', alignItems: 'center', gap: 14, textAlign: 'left', width: '100%',
                      padding: '12px 14px', cursor: 'pointer',
                      background: activa ? 'var(--brand-50)' : 'var(--surface)',
                      border: `1px solid ${activa ? 'var(--brand)' : 'var(--hairline-2)'}`,
                      borderRadius: 'var(--r-3)',
                    }}
                  >
                    <span style={{ flex: 1, minWidth: 0 }}>
                      <span style={{ display: 'block', fontWeight: 'var(--fw-m)', fontSize: 'var(--fs-sm)' }}>
                        {spec.label}
                      </span>
                      <span style={{ display: 'block', fontSize: 'var(--fs-xs)', color: 'var(--muted)', marginTop: 2 }}>
                        {spec.hint}
                      </span>
                    </span>
                    {activa ? <IcCheck size={16} style={{ color: 'var(--brand)' }} /> : null}
                  </button>
                )
              })}
            </div>
          </div>

          <div className="card">
            <div className="card-h">
              <div>
                <h3>Textos</h3>
                <div className="sub">vacío = el texto del diseño</div>
              </div>
            </div>
            <div className="card-b" style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
              <Campo label="Título de la portada" hint="Lo más grande de la página de inicio.">
                <input className="input" value={heroTitle} maxLength={160}
                  placeholder={PLACEHOLDERS.heroTitle}
                  onChange={(e) => setHeroTitle(e.target.value)} />
              </Campo>
              <Campo label="Bajada de la portada" hint="Una o dos líneas debajo del título.">
                <textarea className="input" style={{ height: 72, paddingTop: 8, resize: 'vertical' }}
                  value={heroSubtitle} maxLength={400}
                  placeholder={PLACEHOLDERS.heroSubtitle}
                  onChange={(e) => setHeroSubtitle(e.target.value)} />
              </Campo>
              <Campo label="La empresa" hint="Se ve en /nosotros. Podés separar párrafos con saltos de línea.">
                <textarea className="input" style={{ height: 130, paddingTop: 8, resize: 'vertical' }}
                  value={aboutText} maxLength={2000}
                  placeholder={PLACEHOLDERS.aboutText}
                  onChange={(e) => setAboutText(e.target.value)} />
              </Campo>
              <Campo label="Frase del pie" hint="Debajo del nombre, en el pie de página.">
                <input className="input" value={footerTagline} maxLength={200}
                  placeholder={PLACEHOLDERS.footerTagline}
                  onChange={(e) => setFooterTagline(e.target.value)} />
              </Campo>
            </div>
          </div>

          <div className="card">
            <div className="card-h"><h3>Logo y datos de contacto</h3></div>
            <div className="card-b" style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              <div style={{ fontSize: 'var(--fs-sm)', color: 'var(--muted)', lineHeight: 1.5 }}>
                El logo del encabezado y del pie, la dirección, el teléfono y el email salen de
                la marca de la inmobiliaria — la misma que va impresa en los recibos. Sin logo
                cargado, el sitio muestra un monograma con la inicial del nombre.
              </div>
              <Link className="btn" to="/admin/configuracion/marca" style={{ alignSelf: 'flex-start' }}>
                Editar marca
              </Link>
            </div>
          </div>

          {update.isSuccess ? (
            <div role="status" style={{ fontSize: 'var(--fs-sm)', color: 'var(--ok)' }}>
              Guardado. Recargá tu sitio para verlo.
            </div>
          ) : null}
          {update.isError ? (
            <div role="alert" style={{ fontSize: 'var(--fs-sm)', color: 'var(--danger)' }}>
              No pudimos guardar los cambios. Revisá el color y probá de nuevo.
            </div>
          ) : null}
        </div>
      </div>
    </>
  )
}

function Campo({ label, hint, children }: { label: string; hint: string; children: React.ReactNode }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
      <label className="label">{label}</label>
      {children}
      <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>{hint}</span>
    </div>
  )
}
