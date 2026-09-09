import { api } from '@/shared/lib/api'

/**
 * `PropertyPhotoDto.url` llega relativa al ORIGIN de la API (`/api/v1/public/{slug}/photos/{id}`),
 * no al `baseURL` del cliente axios (que ya incluye el prefijo `/api/v1`). En dev el baseURL es
 * `http://localhost:5000/api/v1`, así que resolvemos contra su origin (`http://localhost:5000`)
 * para no terminar duplicando `/api/v1` en la URL final.
 */
export function resolvePropertyPhotoUrl(url: string): string {
  const base = api.defaults.baseURL ?? window.location.origin
  const origin = new URL(base, window.location.origin).origin
  return new URL(url, origin).toString()
}
