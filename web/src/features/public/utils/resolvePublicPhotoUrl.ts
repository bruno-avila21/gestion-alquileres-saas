import { publicApi } from '@/shared/lib/publicApi'

/**
 * `coverPhotoUrl` y `photoUrls[]` llegan RELATIVAS al ORIGIN de la API
 * (`/api/v1/public/{slug}/photos/{id}`), no al `baseURL` del cliente axios (que ya incluye
 * el prefijo `/api/v1`). Resolvemos contra el origin para no duplicar `/api/v1`.
 *
 * Equivalente público de `features/properties/utils/resolvePhotoUrl.ts` (que usa el cliente
 * `api` del panel admin) — este usa `publicApi`, que es el cliente anónimo del sitio público.
 */
export function resolvePublicPhotoUrl(url: string): string {
  const base = publicApi.defaults.baseURL ?? window.location.origin
  const origin = new URL(base, window.location.origin).origin
  return new URL(url, origin).toString()
}

/**
 * URL del logo de la inmobiliaria. Es un `<img src>` directo y no una descarga por axios a
 * propósito: el navegador lo cachea con el `Cache-Control` de un día que manda el endpoint,
 * y no hace falta un blob que se regenere en cada montaje del layout.
 */
export function publicLogoUrl(slug: string): string {
  const base = publicApi.defaults.baseURL ?? window.location.origin
  return new URL(`${base.replace(/\/$/, '')}/public/${encodeURIComponent(slug)}/logo`, window.location.origin).toString()
}
