import { useOrganization, useUpdateOrganization } from '@/features/organization/hooks/useOrganization'
import {
  PANEL_PALETTES, PANEL_PALETTE_LABELS,
  type PanelPalette,
} from '@/features/organization/types/organization.types'
import { IcCheck } from '@/shared/components/ui/Icons'

/**
 * Elige la paleta del panel. Guarda en la organización, así que la eligen una vez
 * y la ve todo el equipo en cualquier máquina — a diferencia de una preferencia
 * de navegador, que se pierde al cambiar de computadora.
 *
 * La vista previa de cada opción no es un cuadradito de color: son las tres piezas
 * que el color realmente toca (botón de acción, ítem activo del menú, chip), para
 * que se vea qué cambia antes de cambiarlo.
 */
export function PalettePicker() {
  const { data: org, isLoading } = useOrganization()
  const update = useUpdateOrganization()

  function choose(palette: PanelPalette) {
    if (!org || palette === org.panelPalette) return
    update.mutate({
      name: org.name,
      legalName: org.legalName,
      taxId: org.taxId,
      address: org.address,
      phone: org.phone,
      email: org.email,
      brandColor: org.brandColor,
      panelPalette: palette,
    })
  }

  if (isLoading || !org) {
    return <div style={{ color: 'var(--muted)', fontSize: 'var(--fs-sm)' }}>Cargando…</div>
  }

  return (
    <div role="radiogroup" aria-label="Paleta del panel" style={{ display: 'grid', gap: 10 }}>
      {PANEL_PALETTES.map((p) => {
        const meta = PANEL_PALETTE_LABELS[p]
        const active = org.panelPalette === p
        return (
          <button
            key={p}
            type="button"
            role="radio"
            aria-checked={active}
            disabled={update.isPending}
            onClick={() => choose(p)}
            data-palette={p}
            style={{
              display: 'flex', alignItems: 'center', gap: 14, width: '100%',
              padding: '12px 14px', textAlign: 'left',
              background: active ? 'var(--brand-50)' : 'var(--surface)',
              border: `1px solid ${active ? 'var(--brand)' : 'var(--hairline-2)'}`,
              borderRadius: 'var(--r-3)',
              cursor: update.isPending ? 'wait' : 'pointer',
              transition: 'border-color .15s, background .15s',
            }}
          >
            {/* Vista previa: botón, ítem de menú y chip, con el color de ESA paleta. */}
            <span aria-hidden="true" style={{ display: 'flex', alignItems: 'center', gap: 6, flexShrink: 0 }}>
              <span style={{
                width: 34, height: 22, borderRadius: 6, background: 'var(--brand)',
              }} />
              <span style={{
                width: 22, height: 22, borderRadius: 6, background: 'var(--brand-50)',
                border: '1px solid var(--brand-100)',
              }} />
              <span style={{
                width: 10, height: 22, borderRadius: 6, background: 'var(--brand-700)',
              }} />
            </span>

            <span style={{ flex: 1, minWidth: 0 }}>
              <span style={{ display: 'block', fontWeight: 'var(--fw-m)', fontSize: 'var(--fs-sm)' }}>
                {meta.name}
                {p === 'azul' ? (
                  <span style={{ color: 'var(--muted)', fontWeight: 400 }}> · predeterminada</span>
                ) : null}
              </span>
              <span style={{ display: 'block', fontSize: 'var(--fs-xs)', color: 'var(--muted)', marginTop: 2 }}>
                {meta.hint}
              </span>
            </span>

            {active ? (
              <span style={{ color: 'var(--brand)', flexShrink: 0, display: 'grid' }}>
                <IcCheck size={16} />
              </span>
            ) : null}
          </button>
        )
      })}

      {update.isError ? (
        <div role="alert" style={{ fontSize: 'var(--fs-xs)', color: 'var(--danger)' }}>
          No pudimos guardar la paleta. Probá de nuevo.
        </div>
      ) : null}
    </div>
  )
}
