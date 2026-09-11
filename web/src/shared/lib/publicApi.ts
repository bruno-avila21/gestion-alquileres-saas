import axios from 'axios'

/**
 * Cliente HTTP para el sitio público de la inmobiliaria (`/sitio/:slug/*`).
 *
 * A diferencia de `api` (shared/lib/api.ts), este cliente:
 *   - NO manda cookies de sesión (`withCredentials` ausente) — los endpoints `/public/{slug}/*`
 *     son anónimos, no hay usuario logueado.
 *   - NO tiene interceptor de 401 → no debe redirigir nunca a `/admin/login`. Un 404 en
 *     `/public/{slug}` significa "organización inexistente o inactiva", no una sesión vencida.
 */
const baseURL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000/api/v1'

export const publicApi = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
})
