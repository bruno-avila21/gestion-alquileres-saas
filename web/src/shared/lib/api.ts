import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { useAuthStore } from '@/shared/stores/authStore'

const baseURL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000/api/v1'

export const api = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
  // Send the HttpOnly auth cookie with every request. The JWT is no longer kept in
  // localStorage, so there is no Authorization header to attach client-side.
  withCredentials: true,
})

/** Marca interna para no reintentar dos veces la misma petición. */
type RetriableConfig = InternalAxiosRequestConfig & { _retried?: boolean }

/**
 * Renovación de sesión compartida.
 *
 * Si varias peticiones reciben 401 a la vez —lo habitual, porque una pantalla dispara varias
 * consultas juntas— todas esperan la MISMA llamada a /auth/refresh en vez de disparar una cada una.
 * Sin esta deduplicación, la rotación del refresh token del backend interpretaría las llamadas
 * simultáneas como reuso y revocaría toda la familia, cerrando la sesión justo cuando intentábamos
 * salvarla.
 */
let refreshInFlight: Promise<void> | null = null

function refreshSession(): Promise<void> {
  refreshInFlight ??= api
    .post('/auth/refresh')
    .then(() => undefined)
    .finally(() => {
      refreshInFlight = null
    })
  return refreshInFlight
}

function redirectToLogin() {
  useAuthStore.getState().logout()
  const path = window.location.pathname
  const target = path.startsWith('/inquilino') ? '/inquilino/login' : '/admin/login'
  if (path !== target) window.location.href = target
}

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as RetriableConfig | undefined
    const isAuthCall = (original?.url ?? '').includes('/auth/')

    if (error.response?.status !== 401 || !original || original._retried || isAuthCall) {
      // Un 401 en /auth/* es un login fallido o un refresh vencido: no hay nada que renovar.
      if (error.response?.status === 401) redirectToLogin()
      return Promise.reject(error)
    }

    // El access token dura minutos, no horas: expirar a mitad de sesión es lo esperable, no una
    // anomalía. Se renueva y se reintenta, en vez de expulsar al usuario y perderle el formulario.
    original._retried = true
    try {
      await refreshSession()
      return await api(original)
    } catch {
      redirectToLogin()
      return await Promise.reject(error)
    }
  },
)
