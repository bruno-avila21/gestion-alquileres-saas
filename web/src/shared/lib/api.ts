import axios, { AxiosError } from 'axios'
import { useAuthStore } from '@/shared/stores/authStore'

const baseURL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000/api/v1'

export const api = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
  // Send the HttpOnly auth cookie with every request. The JWT is no longer kept in
  // localStorage, so there is no Authorization header to attach client-side.
  withCredentials: true,
})

api.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (error.response?.status === 401) {
      useAuthStore.getState().logout()
      // Portal-aware redirect: if current path is /inquilino/*, go to tenant login
      const path = window.location.pathname
      const target = path.startsWith('/inquilino') ? '/inquilino/login' : '/admin/login'
      if (path !== target) {
        window.location.href = target
      }
    }
    return Promise.reject(error)
  },
)
