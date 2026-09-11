import { useEffect } from 'react'
import { FONT_PAIRINGS, type SiteFontPairing } from '@/features/public/utils/siteTheme'

const LINK_ID = 'pp-site-fonts'

/**
 * Carga las tipografías de la combinación elegida, y sólo ésa.
 *
 * Se inyecta el `<link>` en tiempo de ejecución en vez de listar las tres familias en
 * index.html porque ese archivo lo comparten el panel y los dos portales: pedirle a
 * Google Fonts las tres combinaciones en cada carga haría que todos paguen el peso de
 * las fuentes que sólo usa una inmobiliaria.
 *
 * La combinación por defecto no inyecta nada: Plus Jakarta Sans ya viene en index.html.
 */
export function useSiteFonts(pairing: SiteFontPairing | null | undefined) {
  useEffect(() => {
    const existing = document.getElementById(LINK_ID)

    if (!pairing || pairing === 'jakarta') {
      existing?.remove()
      return
    }

    const families = FONT_PAIRINGS[pairing].googleFamilies
      .map((f) => `family=${f}`)
      .join('&')
    const href = `https://fonts.googleapis.com/css2?${families}&display=swap`

    if (existing instanceof HTMLLinkElement) {
      if (existing.href !== href) existing.href = href
      return
    }

    const link = document.createElement('link')
    link.id = LINK_ID
    link.rel = 'stylesheet'
    link.href = href
    document.head.appendChild(link)
  }, [pairing])
}
