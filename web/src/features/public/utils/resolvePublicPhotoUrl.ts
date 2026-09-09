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
