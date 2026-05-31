import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export type UserRole = 'Admin' | 'Staff' | 'Tenant'

export interface AuthUser {
  userId: string
  email: string
  role: UserRole
  organizationId: string
  organizationSlug: string
}

interface AuthState {
  user: AuthUser | null
  login: (user: AuthUser) => void
  logout: () => void
}

const apiBase = import.meta.env.VITE_API_URL ?? 'http://localhost:5000/api/v1'

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      // The JWT now lives in an HttpOnly cookie set by the API — it is never stored in JS.
      // Only the non-sensitive user profile is persisted, for UI and route guards.
      login: (user) => set({ user }),
      logout: () => {
        // Best-effort: clear the HttpOnly auth cookie on the server.
        void fetch(`${apiBase}/auth/logout`, { method: 'POST', credentials: 'include' }).catch(() => {})
        set({ user: null })
      },
    }),
    {
      name: 'gestion-alquileres-auth',
      partialize: (state) => ({ user: state.user }),
    },
  ),
)
