/**
 * Paletas del panel. La lista es cerrada y la valida el API (Domain/PanelPalettes.cs):
 * cada una está calibrada para que el contraste del texto sobre el color de acción y
 * los estados de foco sigan siendo legibles. El color libre de la inmobiliaria es otro
 * campo, `brandColor`, y sólo viaja impreso en los PDF.
 */
export const PANEL_PALETTES = ['azul', 'violeta', 'carbon', 'vino'] as const
export type PanelPalette = (typeof PANEL_PALETTES)[number]

export const PANEL_PALETTE_LABELS: Record<PanelPalette, { name: string; hint: string; swatch: string }> = {
  azul: { name: 'Azul', hint: 'Institucional y sobrio. El que trae el producto.', swatch: '#2f4ad0' },
  violeta: { name: 'Violeta', hint: 'Más moderno, con más presencia.', swatch: '#6d28d9' },
  carbon: { name: 'Carbón', hint: 'Neutro, sin color de marca.', swatch: '#1f1f1f' },
  vino: { name: 'Vino', hint: 'Cálido y tradicional.', swatch: '#8a2a3e' },
}

export interface OrganizationDto {
  id: string
  name: string
  legalName: string | null
  taxId: string | null
  address: string | null
  phone: string | null
  email: string | null
  brandColor: string | null
  panelPalette: PanelPalette
  hasLogo: boolean
  plan: string
}

export interface UpdateOrganizationRequest {
  name: string
  legalName: string | null
  taxId: string | null
  address: string | null
  phone: string | null
  email: string | null
  brandColor: string | null
  panelPalette: PanelPalette | null
}
