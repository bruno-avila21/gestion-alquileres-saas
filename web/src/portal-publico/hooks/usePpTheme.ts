import { useCallback, useEffect, useState } from 'react'

const STORAGE_KEY = 'pp-theme'

type PpTheme = 'light' | 'dark' | null

function readStoredTheme(): PpTheme {
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY)
    return raw === 'light' || raw === 'dark' ? raw : null
  } catch {
    return null
  }
}

/**
 * Tema del sitio público, independiente del panel admin. `null` = claro, el tema por defecto
 * del sistema de diseño (el CSS ya no sigue `prefers-color-scheme`: el modelo es light-first
 * y una inmobiliaria se muestra de día). `light`/`dark` = elección explícita persistida.
 */
export function usePpTheme() {
  const [theme, setTheme] = useState<PpTheme>(() => readStoredTheme())

  useEffect(() => {
    try {
      if (theme) window.localStorage.setItem(STORAGE_KEY, theme)
      else window.localStorage.removeItem(STORAGE_KEY)
    } catch {
      // localStorage puede no estar disponible (modo privado); el tema sigue funcionando en memoria.
    }
  }, [theme])

  const toggleTheme = useCallback(() => {
    setTheme((current) => (current === 'dark' ? 'light' : 'dark'))
  }, [])

  return { theme, toggleTheme }
}
