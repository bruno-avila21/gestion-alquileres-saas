const STORAGE_KEY = 'last-org-slug'

/**
 * Última organización con la que se ingresó desde este navegador. Sirve para
 * precargar el campo "Organización" del login, que es el dato que nadie
 * recuerda y el que más veces provoca un 401 que parece contraseña mala.
 * No es un secreto: es el mismo slug que aparece en la URL del sitio público.
 */
export function readLastOrgSlug(): string | null {
  try {
    return window.localStorage.getItem(STORAGE_KEY)
  } catch {
    return null
  }
}

export function rememberOrgSlug(slug: string): void {
  try {
    if (slug) window.localStorage.setItem(STORAGE_KEY, slug)
  } catch {
    // localStorage puede no estar disponible (modo privado); el login funciona igual.
  }
}
