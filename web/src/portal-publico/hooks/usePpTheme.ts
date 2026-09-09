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
 * Tema del sitio público, independiente del panel admin. `null` = seguir el tema del sistema
 * (`prefers-color-scheme`, resuelto en CSS); `light`/`dark` = elección explícita persistida.
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
    setTheme((current) => {
      const isDark = current === 'dark' || (current === null && window.matchMedia('(prefers-color-scheme: dark)').matches)
      return isDark ? 'light' : 'dark'
    })
  }, [])

  return { theme, toggleTheme }
}
