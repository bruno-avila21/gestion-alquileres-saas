/**
 * Aspecto y textos del sitio público, elegidos por la inmobiliaria.
 *
 * Todo campo en null significa "usá lo que trae el diseño". Los valores por defecto viven
 * acá y no en la base a propósito: así el diseño por defecto puede cambiar sin migrar una
 * fila por cada organización, y una inmobiliaria que no tocó nada se lleva las mejoras.
 */

export const SITE_FONT_PAIRINGS = ['jakarta', 'editorial', 'compacta'] as const
export type SiteFontPairing = (typeof SITE_FONT_PAIRINGS)[number]

export interface SiteSettingsDto {
  accentColor: string | null
  fontPairing: SiteFontPairing | null
  heroTitle: string | null
  heroSubtitle: string | null
  aboutText: string | null
  footerTagline: string | null
}

/** El violeta del sistema de diseño aprobado. */
export const DEFAULT_ACCENT = '#6d28d9'

interface FontPairingSpec {
  label: string
  hint: string
  /** Familias a pedirle a Google Fonts, con sus pesos. */
  googleFamilies: string[]
  headings: string
  body: string
}

export const FONT_PAIRINGS: Record<SiteFontPairing, FontPairingSpec> = {
  jakarta: {
    label: 'Jakarta',
    hint: 'Geométrica y clara. La del diseño por defecto.',
    googleFamilies: ['Plus+Jakarta+Sans:wght@400;500;600;700;800'],
    headings: '"Plus Jakarta Sans", system-ui, sans-serif',
    body: '"Plus Jakarta Sans", system-ui, sans-serif',
  },
  editorial: {
    label: 'Editorial',
    hint: 'Títulos con serifa. Más tradicional, para una inmobiliaria de años.',
    googleFamilies: ['Fraunces:opsz,wght@9..144,600;9..144,700', 'Plus+Jakarta+Sans:wght@400;500;600'],
    headings: 'Fraunces, Georgia, serif',
    body: '"Plus Jakarta Sans", system-ui, sans-serif',
  },
  compacta: {
    label: 'Compacta',
    hint: 'Seca y angosta. Entra más texto en la misma tarjeta.',
    googleFamilies: ['Archivo:wght@400;500;600;700;800'],
    headings: 'Archivo, system-ui, sans-serif',
    body: 'Archivo, system-ui, sans-serif',
  },
}

/**
 * Tinta legible sobre un color de fondo: blanco o casi negro, el que más contraste dé.
 *
 * Hace falta porque el color de acento lo elige la inmobiliaria y va abajo del texto de los
 * botones. Con blanco fijo, un acento claro —un amarillo, un lima— deja el botón principal
 * ilegible y nadie se entera hasta que un visitante no encuentra "Consultar".
 *
 * Usa luminancia relativa de la WCAG, que es la misma cuenta que hace un medidor de
 * contraste, y no el promedio de R/G/B: el ojo ve el verde mucho más que el azul, y
 * promediar da un resultado que se equivoca justo en los tonos del medio.
 */
export function inkOn(hex: string): string {
  const rgb = hexToRgb(hex)
  if (!rgb) return '#ffffff'

  const channel = (v: number) => {
    const c = v / 255
    return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4
  }
  const L = 0.2126 * channel(rgb.r) + 0.7152 * channel(rgb.g) + 0.0722 * channel(rgb.b)

  // Contraste contra blanco y contra la tinta oscura del sistema; gana el mayor.
  const contraWhite = 1.05 / (L + 0.05)
  const contraDark = (L + 0.05) / 0.05
  return contraWhite >= contraDark ? '#ffffff' : '#0f172a'
}

/** Aclara u oscurece un hex mezclándolo con blanco o negro. `amount` en 0..1. */
export function shade(hex: string, amount: number): string {
  const rgb = hexToRgb(hex)
  if (!rgb) return hex
  const target = amount < 0 ? 0 : 255
  const k = Math.abs(amount)
  const mix = (v: number) => Math.round(v + (target - v) * k)
  return rgbToHex(mix(rgb.r), mix(rgb.g), mix(rgb.b))
}

/** Mezcla dos hex. `amount` 0 = todo `from`, 1 = todo `to`. */
function mixHex(from: string, to: string, amount: number): string {
  const a = hexToRgb(from)
  const b = hexToRgb(to)
  if (!a || !b) return from
  const mix = (x: number, y: number) => Math.round(x + (y - x) * amount)
  return rgbToHex(mix(a.r, b.r), mix(a.g, b.g), mix(a.b, b.b))
}

function hexToRgb(hex: string): { r: number; g: number; b: number } | null {
  const m = /^#?([0-9a-f]{6})$/i.exec(hex.trim())
  if (!m) return null
  const n = parseInt(m[1], 16)
  return { r: (n >> 16) & 255, g: (n >> 8) & 255, b: n & 255 }
}

function rgbToHex(r: number, g: number, b: number): string {
  return `#${[r, g, b].map((v) => v.toString(16).padStart(2, '0')).join('')}`
}

/** La superficie del tema oscuro (`--surface` de publico.css). Los tintes oscuros se mezclan
 *  contra ella y no contra negro: un violeta mezclado con negro se ensucia y pierde el tono. */
const DARK_SURFACE = '#17202c'

/**
 * Las variables CSS que hay que pisar en `.pp-app` para aplicar el acento elegido.
 * Devuelve un objeto de estilo, no toca el DOM: así el editor puede usarlo para la vista
 * previa sin ensuciar el documento del panel.
 *
 * Recibe el tema porque estas variables se inyectan EN LÍNEA en el nodo raíz del sitio, y un
 * estilo en línea le gana a cualquier regla de la hoja: si devolviéramos siempre las
 * derivaciones claras, el bloque `[data-theme="dark"]` de publico.css no podría redefinirlas
 * y el modo oscuro se quedaba con los tintes casi blancos del modo claro (texto blanco sobre
 * fondo lila ilegible en la cabecera de precio de la ficha).
 */
export function accentVars(
  accent: string | null | undefined,
  theme?: 'light' | 'dark' | null,
): React.CSSProperties {
  const base = accent?.trim() || DEFAULT_ACCENT
  if (!/^#[0-9a-f]{6}$/i.test(base)) return {}

  if (theme === 'dark') {
    // En oscuro el acento se aclara para separarse del fondo, y los tintes se hunden hacia
    // la superficie. Es la misma relación que tiene la paleta oscura de publico.css.
    const violet = shade(base, 0.45)
    return {
      '--violet': violet,
      '--violet-deep': shade(base, 0.3),
      '--violet-bright': shade(base, 0.62),
      '--violet-tint': mixHex(base, DARK_SURFACE, 0.84),
      '--violet-edge': mixHex(base, DARK_SURFACE, 0.62),
      '--on-violet': inkOn(violet),
    } as React.CSSProperties
  }

  return {
    '--violet': base,
    '--violet-deep': shade(base, -0.18),
    '--violet-bright': shade(base, 0.12),
    '--violet-tint': shade(base, 0.92),
    '--violet-edge': shade(base, 0.72),
    '--on-violet': inkOn(base),
  } as React.CSSProperties
}
