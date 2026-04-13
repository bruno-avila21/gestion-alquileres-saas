---
id: 1-04
title: React 19 + Vite + Frontend Foundation + Login Page
wave: 1
depends_on: []
files_modified:
  - web/package.json
  - web/pnpm-lock.yaml
  - web/tsconfig.json
  - web/tsconfig.app.json
  - web/tsconfig.node.json
  - web/vite.config.ts
  - web/index.html
  - web/src/main.tsx
  - web/src/App.tsx
  - web/src/index.css
  - web/src/vite-env.d.ts
  - web/.env.example
  - web/components.json
  - web/src/shared/lib/api.ts
  - web/src/shared/lib/queryClient.ts
  - web/src/shared/stores/authStore.ts
  - web/src/shared/components/ui/button.tsx
  - web/src/shared/components/ui/input.tsx
  - web/src/shared/components/ui/label.tsx
  - web/src/shared/components/ui/card.tsx
  - web/src/features/auth/types/auth.types.ts
  - web/src/features/auth/services/authService.ts
  - web/src/features/auth/hooks/useLogin.ts
  - web/src/features/auth/hooks/useRegisterOrg.ts
  - web/src/features/auth/hooks/useTenantLogin.ts
  - web/src/features/auth/components/LoginForm.tsx
  - web/src/features/auth/components/RegisterOrgForm.tsx
  - web/src/portal-admin/layouts/AdminLayout.tsx
  - web/src/portal-admin/pages/LoginPage.tsx
  - web/src/portal-admin/pages/RegisterOrgPage.tsx
  - web/src/portal-admin/pages/DashboardPage.tsx
  - web/src/portal-admin/routes.tsx
  - web/src/portal-inquilino/layouts/InquilinoLayout.tsx
  - web/src/portal-inquilino/pages/LoginPage.tsx
  - web/src/portal-inquilino/routes.tsx
  - web/src/features/auth/__tests__/LoginForm.test.tsx
autonomous: true
requirements: [ORG-04, ORG-05]
must_haves:
  truths:
    - "pnpm install && pnpm build completes without errors"
    - "TypeScript strict mode is enabled (no `any` allowed)"
    - "Tailwind v4 applies styles through @tailwindcss/vite plugin, NOT tailwind.config.js"
    - "Axios client attaches JWT from zustand store to Authorization header"
    - "Axios 401 response triggers logout + redirect to /admin/login"
    - "Admin login page at /admin/login and Tenant login page at /inquilino/login render separately"
    - "useLogin hook uses TanStack Query useMutation (not useEffect) for data fetching"
  artifacts:
    - path: "web/vite.config.ts"
      provides: "Vite config with @tailwindcss/vite plugin and @/ path alias"
    - path: "web/tsconfig.app.json"
      provides: "TS strict mode + @/* path mapping"
    - path: "web/src/shared/lib/api.ts"
      provides: "Axios instance with JWT interceptors"
    - path: "web/src/shared/stores/authStore.ts"
      provides: "Zustand auth store persisted to localStorage"
    - path: "web/src/portal-admin/pages/LoginPage.tsx"
      provides: "Admin login form"
    - path: "web/src/portal-inquilino/pages/LoginPage.tsx"
      provides: "Tenant login form (separate from admin)"
    - path: "web/src/features/auth/services/authService.ts"
      provides: "authService.login, authService.registerOrg, authService.tenantLogin"
  key_links:
    - from: "web/src/shared/lib/api.ts"
      to: "authStore.token"
      via: "axios interceptor reads useAuthStore.getState().token"
      pattern: "useAuthStore.getState\\(\\).token"
    - from: "useLogin hook"
      to: "authService.login"
      via: "TanStack Query useMutation"
      pattern: "useMutation"
    - from: "LoginForm"
      to: "useLogin"
      via: "form onSubmit triggers mutation.mutate"
      pattern: "mutation.mutate"
---

# Plan 1-04: React 19 + Vite + Frontend Foundation + Login Page

## Objective

Scaffold the React 19 + Vite + TypeScript frontend with strict typing, Tailwind v4, TanStack Query, React Router v7, Zustand, and shadcn/ui. Wire Axios interceptors that auto-attach JWT and handle 401 logout. Build separate login pages for the admin portal (`/admin/login`, `/admin/register-org`) and the tenant portal (`/inquilino/login`), matching the dual-portal architecture from the ERD.

**Purpose:** Supports ORG-04 (admin JWT login UI) and ORG-05 (tenant JWT login UI). Runs in Wave 1 fully parallel to Plan 01 (.NET scaffold) — zero file conflicts. The login UI is stubbed against the API contract; it will naturally connect to the real endpoints built in Plan 03 since the DTO shape is already fixed by ERD-AND-API.md.

**Output:** `cd web && pnpm build` exits 0; `pnpm dev` starts at http://localhost:5173 with routable `/admin/login` and `/inquilino/login` pages; `pnpm test` runs vitest tests for form validation.

## must_haves

- [ ] `web/package.json` declares React 19.x, Vite 5+/6+/8+, TypeScript 5+, Tailwind 4.x, TanStack Query 5.x, React Router 7.x, Zustand 5.x, Axios, react-hook-form, zod
- [ ] `web/vite.config.ts` includes `@tailwindcss/vite` plugin and `@` path alias
- [ ] `web/tsconfig.app.json` has `"strict": true` and `"paths": { "@/*": ["./src/*"] }`
- [ ] NO `web/tailwind.config.js` file exists (Tailwind v4 uses the Vite plugin)
- [ ] NO `web/postcss.config.js` file exists
- [ ] `web/src/index.css` uses `@import "tailwindcss"` (v4 syntax)
- [ ] `web/src/shared/lib/api.ts` exports an axios instance with request interceptor attaching `Bearer {token}` and response interceptor handling 401
- [ ] `web/src/shared/stores/authStore.ts` exports `useAuthStore` (Zustand with `persist`)
- [ ] `web/src/portal-admin/pages/LoginPage.tsx` renders a form with email/password/orgSlug
- [ ] `web/src/portal-inquilino/pages/LoginPage.tsx` renders a SEPARATE tenant login
- [ ] `web/src/features/auth/hooks/useLogin.ts` uses `useMutation` from `@tanstack/react-query` (NOT `useEffect`)
- [ ] `cd web && pnpm build` exits 0
- [ ] `cd web && pnpm test --run` exits 0 with at least 1 passing test

## Tasks

<task id="1-04-01">
<title>Task 1: Scaffold Vite + React 19 + TS strict + Tailwind v4 + path alias + install deps</title>
<read_first>
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 128-160) — exact frontend package versions
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 213-225) — frontend scaffold commands
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 625-653) — Tailwind v4 + Vite config
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 786-824) — pitfalls: Tailwind v4 config, shadcn path alias
- CLAUDE.md — TypeScript strict rule (no `any`, no `@ts-ignore`)
- web/AGENTS.md — project conventions for frontend
</read_first>
<action>
Working directory: `C:/Users/Bruno Avila/Documents/Proyectos_Propios/gestion-alquileres-saas/web/`

**Step 1 — Scaffold Vite project inside existing `web/` directory.** `web/` currently contains only `AGENTS.md`. Run from `web/`:

```bash
pnpm create vite@latest . --template react-ts
```

When prompted about non-empty directory, accept (it should skip existing `AGENTS.md`). If the CLI refuses, manually scaffold by running `pnpm create vite@latest scratch --template react-ts` in a temp sibling folder, then copy all files from `scratch/` into `web/` except `AGENTS.md`, then delete `scratch/`.

**Step 2 — Install runtime dependencies:**

```bash
pnpm add react@^19.0.0 react-dom@^19.0.0
pnpm add @tanstack/react-query@^5.0.0
pnpm add react-router@^7.0.0
pnpm add zustand@^5.0.0
pnpm add axios@^1.7.0
pnpm add react-hook-form@^7.50.0
pnpm add zod@^3.23.0
pnpm add @hookform/resolvers@^3.9.0
pnpm add tailwindcss@^4.0.0 @tailwindcss/vite@^4.0.0
pnpm add lucide-react@latest
pnpm add clsx tailwind-merge class-variance-authority
```

**Step 3 — Install dev dependencies:**

```bash
pnpm add -D @vitejs/plugin-react@latest
pnpm add -D typescript@^5.5.0
pnpm add -D @types/react@^19.0.0 @types/react-dom@^19.0.0 @types/node@^22.0.0
pnpm add -D vitest@^2.0.0 jsdom@latest
pnpm add -D @testing-library/react@^16.0.0 @testing-library/user-event@^14.0.0 @testing-library/jest-dom@^6.0.0
pnpm add -D eslint@^9.0.0 @eslint/js@^9.0.0 typescript-eslint@latest eslint-plugin-react-hooks@^5.0.0 eslint-plugin-react-refresh@latest
```

If any version does not resolve, use the closest stable. Record versions chosen in SUMMARY. Goal: React 19 major, Tailwind v4 major, Vitest 2+, TanStack Query 5, React Router 7, Zustand 5.

**Step 4 — Replace `web/vite.config.ts`** with:

```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'node:path'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5173,
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/vitest.setup.ts'],
  },
})
```

**Step 5 — Replace `web/tsconfig.app.json`** (merge if exists) with:

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "useDefineForClassFields": true,
    "lib": ["ES2022", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "skipLibCheck": true,
    "moduleResolution": "bundler",
    "allowImportingTsExtensions": true,
    "resolveJsonModule": true,
    "isolatedModules": true,
    "moduleDetection": "force",
    "noEmit": true,
    "jsx": "react-jsx",
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true,
    "noUncheckedSideEffectImports": true,
    "baseUrl": ".",
    "paths": {
      "@/*": ["./src/*"]
    }
  },
  "include": ["src"]
}
```

**Step 6 — Replace `web/tsconfig.json`** with:

```json
{
  "files": [],
  "references": [
    { "path": "./tsconfig.app.json" },
    { "path": "./tsconfig.node.json" }
  ],
  "compilerOptions": {
    "baseUrl": ".",
    "paths": {
      "@/*": ["./src/*"]
    }
  }
}
```

**Step 7 — Delete v3 artifacts if Vite template created them:**

```bash
rm -f tailwind.config.js tailwind.config.ts postcss.config.js postcss.config.cjs
```

**Step 8 — Replace `web/src/index.css`** with Tailwind v4 import plus CSS variables expected by shadcn/ui:

```css
@import "tailwindcss";

@layer base {
  :root {
    --background: 0 0% 100%;
    --foreground: 222.2 84% 4.9%;
    --card: 0 0% 100%;
    --card-foreground: 222.2 84% 4.9%;
    --primary: 222.2 47.4% 11.2%;
    --primary-foreground: 210 40% 98%;
    --secondary: 210 40% 96.1%;
    --secondary-foreground: 222.2 47.4% 11.2%;
    --muted: 210 40% 96.1%;
    --muted-foreground: 215.4 16.3% 46.9%;
    --border: 214.3 31.8% 91.4%;
    --input: 214.3 31.8% 91.4%;
    --ring: 222.2 84% 4.9%;
    --radius: 0.5rem;
    --destructive: 0 84.2% 60.2%;
    --destructive-foreground: 210 40% 98%;
  }
}

body {
  background-color: hsl(var(--background));
  color: hsl(var(--foreground));
  font-family: system-ui, -apple-system, sans-serif;
}
```

**Step 9 — Create `web/src/vitest.setup.ts`:**
```typescript
import '@testing-library/jest-dom/vitest'
```

**Step 10 — Create `web/.env.example`:**
```
VITE_API_URL=http://localhost:5000/api/v1
```

Also create `web/.env.development` with the same content so `import.meta.env.VITE_API_URL` resolves in dev.

**Step 11 — Add npm scripts.** Ensure `web/package.json` `scripts` section contains:
```json
"scripts": {
  "dev": "vite",
  "build": "tsc -b && vite build",
  "lint": "eslint .",
  "preview": "vite preview",
  "test": "vitest"
}
```

**Step 12 — Verify build:**
```bash
cd web && pnpm install && pnpm build
```
Must exit 0.
</action>
<acceptance_criteria>
- File exists: `web/package.json` and contains `"react": "^19` and `"tailwindcss": "^4` and `"@tanstack/react-query": "^5` and `"react-router": "^7` and `"zustand": "^5` and `"axios"` and `"zod"` and `"react-hook-form"`
- File exists: `web/vite.config.ts` and contains `@tailwindcss/vite` and `'@': path.resolve(__dirname, './src')`
- File exists: `web/tsconfig.app.json` and contains `"strict": true` and `"@/*": ["./src/*"]`
- File exists: `web/src/index.css` and contains `@import "tailwindcss"`
- File exists: `web/.env.example` and contains `VITE_API_URL`
- File exists: `web/src/vitest.setup.ts`
- File does NOT exist: `web/tailwind.config.js`
- File does NOT exist: `web/tailwind.config.ts`
- File does NOT exist: `web/postcss.config.js`
- `web/package.json` scripts section contains `"build": "tsc -b && vite build"`
- `cd web && pnpm install` exits 0
- `cd web && pnpm build` exits 0
</acceptance_criteria>
</task>

<task id="1-04-02">
<title>Task 2: Shared infra — Axios client + Zustand auth store + QueryClient + shadcn/ui primitives + auth feature</title>
<read_first>
- web/vite.config.ts (from Task 1)
- web/tsconfig.app.json (from Task 1)
- web/.env.example (from Task 1)
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 658-688) — Axios interceptors pattern
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 892-917) — Zustand auth store pattern
- .planning/ERD-AND-API.md (lines 219-226) — auth endpoint contracts
- web/AGENTS.md — frontend conventions
- CLAUDE.md — NEVER useEffect for data fetching; always TanStack Query
</read_first>
<action>
**`web/src/shared/stores/authStore.ts`:**
```typescript
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
  token: string | null
  user: AuthUser | null
  login: (token: string, user: AuthUser) => void
  logout: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      user: null,
      login: (token, user) => set({ token, user }),
      logout: () => set({ token: null, user: null }),
    }),
    { name: 'gestion-alquileres-auth' },
  ),
)
```

**`web/src/shared/lib/api.ts`:**
```typescript
import axios, { AxiosError } from 'axios'
import { useAuthStore } from '@/shared/stores/authStore'

const baseURL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000/api/v1'

export const api = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
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
```

**`web/src/shared/lib/queryClient.ts`:**
```typescript
import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      retry: (failureCount, error) => {
        if (error instanceof Error && /401|403/.test(error.message)) return false
        return failureCount < 2
      },
    },
  },
})
```

**`web/src/shared/lib/cn.ts`** (utility for shadcn-style class merging):
```typescript
import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
```

**`web/components.json`** (shadcn/ui config, hand-written to avoid `dlx shadcn init` interactive prompts):
```json
{
  "$schema": "https://ui.shadcn.com/schema.json",
  "style": "default",
  "rsc": false,
  "tsx": true,
  "tailwind": {
    "config": "",
    "css": "src/index.css",
    "baseColor": "slate",
    "cssVariables": true,
    "prefix": ""
  },
  "aliases": {
    "components": "@/shared/components",
    "utils": "@/shared/lib/cn",
    "ui": "@/shared/components/ui",
    "lib": "@/shared/lib",
    "hooks": "@/shared/hooks"
  }
}
```

**Create shadcn-style primitives manually** (no `dlx` needed — these are small and well known):

`web/src/shared/components/ui/button.tsx`:
```tsx
import * as React from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/shared/lib/cn'

const buttonVariants = cva(
  'inline-flex items-center justify-center rounded-md text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 disabled:pointer-events-none disabled:opacity-50',
  {
    variants: {
      variant: {
        default: 'bg-slate-900 text-white hover:bg-slate-800',
        outline: 'border border-slate-300 bg-white hover:bg-slate-50',
        ghost: 'hover:bg-slate-100',
      },
      size: {
        default: 'h-10 px-4 py-2',
        sm: 'h-9 px-3',
        lg: 'h-11 px-8',
      },
    },
    defaultVariants: { variant: 'default', size: 'default' },
  },
)

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {}

export const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, ...props }, ref) => (
    <button ref={ref} className={cn(buttonVariants({ variant, size }), className)} {...props} />
  ),
)
Button.displayName = 'Button'
```

`web/src/shared/components/ui/input.tsx`:
```tsx
import * as React from 'react'
import { cn } from '@/shared/lib/cn'

export type InputProps = React.InputHTMLAttributes<HTMLInputElement>

export const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, type, ...props }, ref) => (
    <input
      type={type}
      ref={ref}
      className={cn(
        'flex h-10 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-900 disabled:cursor-not-allowed disabled:opacity-50',
        className,
      )}
      {...props}
    />
  ),
)
Input.displayName = 'Input'
```

`web/src/shared/components/ui/label.tsx`:
```tsx
import * as React from 'react'
import { cn } from '@/shared/lib/cn'

export const Label = React.forwardRef<
  HTMLLabelElement,
  React.LabelHTMLAttributes<HTMLLabelElement>
>(({ className, ...props }, ref) => (
  <label ref={ref} className={cn('text-sm font-medium leading-none', className)} {...props} />
))
Label.displayName = 'Label'
```

`web/src/shared/components/ui/card.tsx`:
```tsx
import * as React from 'react'
import { cn } from '@/shared/lib/cn'

export const Card = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div ref={ref} className={cn('rounded-lg border border-slate-200 bg-white shadow-sm', className)} {...props} />
  ),
)
Card.displayName = 'Card'

export const CardHeader = ({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) => (
  <div className={cn('flex flex-col gap-1.5 p-6', className)} {...props} />
)
export const CardTitle = ({ className, ...props }: React.HTMLAttributes<HTMLHeadingElement>) => (
  <h3 className={cn('text-2xl font-semibold', className)} {...props} />
)
export const CardContent = ({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) => (
  <div className={cn('p-6 pt-0', className)} {...props} />
)
```

**`web/src/features/auth/types/auth.types.ts`:**
```typescript
export interface LoginRequest {
  email: string
  password: string
  organizationSlug: string
}

export interface RegisterOrgRequest {
  organizationName: string
  slug: string
  adminEmail: string
  adminPassword: string
  adminFirstName: string
  adminLastName: string
}

export interface AuthResponse {
  token: string
  userId: string
  email: string
  role: 'Admin' | 'Staff' | 'Tenant'
  organizationId: string
  organizationSlug: string
}
```

**`web/src/features/auth/services/authService.ts`:**
```typescript
import { api } from '@/shared/lib/api'
import type { AuthResponse, LoginRequest, RegisterOrgRequest } from '../types/auth.types'

export const authService = {
  async login(req: LoginRequest): Promise<AuthResponse> {
    const { data } = await api.post<AuthResponse>('/auth/login', req)
    return data
  },
  async tenantLogin(req: LoginRequest): Promise<AuthResponse> {
    const { data } = await api.post<AuthResponse>('/auth/tenant-login', req)
    return data
  },
  async registerOrg(req: RegisterOrgRequest): Promise<AuthResponse> {
    const { data } = await api.post<AuthResponse>('/auth/register-org', req)
    return data
  },
}
```

**`web/src/features/auth/hooks/useLogin.ts`:**
```typescript
import { useMutation } from '@tanstack/react-query'
import { useNavigate } from 'react-router'
import { authService } from '../services/authService'
import { useAuthStore } from '@/shared/stores/authStore'
import type { LoginRequest } from '../types/auth.types'

export function useLogin() {
  const login = useAuthStore((s) => s.login)
  const navigate = useNavigate()
  return useMutation({
    mutationFn: (req: LoginRequest) => authService.login(req),
    onSuccess: (data) => {
      login(data.token, {
        userId: data.userId,
        email: data.email,
        role: data.role,
        organizationId: data.organizationId,
        organizationSlug: data.organizationSlug,
      })
      navigate('/admin/dashboard', { replace: true })
    },
  })
}
```

**`web/src/features/auth/hooks/useTenantLogin.ts`:**
```typescript
import { useMutation } from '@tanstack/react-query'
import { useNavigate } from 'react-router'
import { authService } from '../services/authService'
import { useAuthStore } from '@/shared/stores/authStore'
import type { LoginRequest } from '../types/auth.types'

export function useTenantLogin() {
  const login = useAuthStore((s) => s.login)
  const navigate = useNavigate()
  return useMutation({
    mutationFn: (req: LoginRequest) => authService.tenantLogin(req),
    onSuccess: (data) => {
      login(data.token, {
        userId: data.userId,
        email: data.email,
        role: data.role,
        organizationId: data.organizationId,
        organizationSlug: data.organizationSlug,
      })
      navigate('/inquilino', { replace: true })
    },
  })
}
```

**`web/src/features/auth/hooks/useRegisterOrg.ts`:**
```typescript
import { useMutation } from '@tanstack/react-query'
import { useNavigate } from 'react-router'
import { authService } from '../services/authService'
import { useAuthStore } from '@/shared/stores/authStore'
import type { RegisterOrgRequest } from '../types/auth.types'

export function useRegisterOrg() {
  const login = useAuthStore((s) => s.login)
  const navigate = useNavigate()
  return useMutation({
    mutationFn: (req: RegisterOrgRequest) => authService.registerOrg(req),
    onSuccess: (data) => {
      login(data.token, {
        userId: data.userId,
        email: data.email,
        role: data.role,
        organizationId: data.organizationId,
        organizationSlug: data.organizationSlug,
      })
      navigate('/admin/dashboard', { replace: true })
    },
  })
}
```

Run `cd web && pnpm build`. Must exit 0.
</action>
<acceptance_criteria>
- File exists: `web/src/shared/stores/authStore.ts` and contains `persist` and `'gestion-alquileres-auth'`
- File exists: `web/src/shared/lib/api.ts` and contains `useAuthStore.getState().token` and `config.headers.Authorization = \`Bearer ${token}\`` and `error.response?.status === 401`
- File exists: `web/src/shared/lib/queryClient.ts` and contains `new QueryClient`
- File exists: `web/src/shared/lib/cn.ts`
- File exists: `web/components.json` and contains `"ui": "@/shared/components/ui"`
- File exists: `web/src/shared/components/ui/button.tsx`
- File exists: `web/src/shared/components/ui/input.tsx`
- File exists: `web/src/shared/components/ui/label.tsx`
- File exists: `web/src/shared/components/ui/card.tsx`
- File exists: `web/src/features/auth/types/auth.types.ts` and contains `organizationSlug`
- File exists: `web/src/features/auth/services/authService.ts` and contains `'/auth/login'` and `'/auth/tenant-login'` and `'/auth/register-org'`
- File exists: `web/src/features/auth/hooks/useLogin.ts` and contains `useMutation` and does NOT contain `useEffect`
- File exists: `web/src/features/auth/hooks/useTenantLogin.ts` and contains `authService.tenantLogin`
- File exists: `web/src/features/auth/hooks/useRegisterOrg.ts`
- Grep `web/src/features/auth/hooks/` for `useEffect` returns ZERO matches
- `cd web && pnpm build` exits 0
</acceptance_criteria>
</task>

<task id="1-04-03">
<title>Task 3: Login/Register forms + routing for dual portals + App.tsx + vitest form test</title>
<read_first>
- web/src/features/auth/hooks/useLogin.ts (from Task 2)
- web/src/features/auth/hooks/useRegisterOrg.ts (from Task 2)
- web/src/features/auth/hooks/useTenantLogin.ts (from Task 2)
- web/src/shared/components/ui/button.tsx (from Task 2)
- web/src/shared/lib/queryClient.ts (from Task 2)
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 920-951) — React Router v7 dual portal routing
- web/AGENTS.md — portal conventions
</read_first>
<action>
**`web/src/features/auth/components/LoginForm.tsx`:**
```tsx
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import type { LoginRequest } from '../types/auth.types'

const schema = z.object({
  organizationSlug: z.string().min(1, 'Slug requerido').regex(/^[a-z0-9-]+$/, 'Slug inválido'),
  email: z.string().email('Email inválido'),
  password: z.string().min(1, 'Contraseña requerida'),
})

type FormValues = z.infer<typeof schema>

interface Props {
  onSubmit: (req: LoginRequest) => void
  isPending: boolean
  errorMessage?: string
  submitLabel: string
}

export function LoginForm({ onSubmit, isPending, errorMessage, submitLabel }: Props) {
  const { register, handleSubmit, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { organizationSlug: '', email: '', password: '' },
  })

  return (
    <form
      onSubmit={handleSubmit((values) => onSubmit(values))}
      className="flex flex-col gap-4"
      aria-label="login-form"
    >
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="organizationSlug">Organización</Label>
        <Input id="organizationSlug" placeholder="acme" {...register('organizationSlug')} />
        {errors.organizationSlug && (
          <span role="alert" className="text-sm text-red-600">{errors.organizationSlug.message}</span>
        )}
      </div>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="email">Email</Label>
        <Input id="email" type="email" autoComplete="email" {...register('email')} />
        {errors.email && <span role="alert" className="text-sm text-red-600">{errors.email.message}</span>}
      </div>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="password">Contraseña</Label>
        <Input id="password" type="password" autoComplete="current-password" {...register('password')} />
        {errors.password && <span role="alert" className="text-sm text-red-600">{errors.password.message}</span>}
      </div>
      {errorMessage && <div role="alert" className="text-sm text-red-600">{errorMessage}</div>}
      <Button type="submit" disabled={isPending}>
        {isPending ? 'Ingresando…' : submitLabel}
      </Button>
    </form>
  )
}
```

**`web/src/features/auth/components/RegisterOrgForm.tsx`:**
```tsx
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import type { RegisterOrgRequest } from '../types/auth.types'

const schema = z.object({
  organizationName: z.string().min(1).max(200),
  slug: z.string().min(1).max(100).regex(/^[a-z0-9-]+$/, 'Slug: solo minúsculas, números y guiones'),
  adminEmail: z.string().email(),
  adminPassword: z.string().min(8, 'Mínimo 8 caracteres').max(100),
  adminFirstName: z.string().min(1).max(100),
  adminLastName: z.string().min(1).max(100),
})

type FormValues = z.infer<typeof schema>

interface Props {
  onSubmit: (req: RegisterOrgRequest) => void
  isPending: boolean
  errorMessage?: string
}

export function RegisterOrgForm({ onSubmit, isPending, errorMessage }: Props) {
  const { register, handleSubmit, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { organizationName: '', slug: '', adminEmail: '', adminPassword: '', adminFirstName: '', adminLastName: '' },
  })

  const fields: Array<{ name: keyof FormValues; label: string; type?: string }> = [
    { name: 'organizationName', label: 'Nombre de organización' },
    { name: 'slug', label: 'Slug (URL)' },
    { name: 'adminFirstName', label: 'Nombre' },
    { name: 'adminLastName', label: 'Apellido' },
    { name: 'adminEmail', label: 'Email admin', type: 'email' },
    { name: 'adminPassword', label: 'Contraseña admin', type: 'password' },
  ]

  return (
    <form onSubmit={handleSubmit((v) => onSubmit(v))} className="flex flex-col gap-4" aria-label="register-org-form">
      {fields.map((f) => (
        <div key={f.name} className="flex flex-col gap-1.5">
          <Label htmlFor={f.name}>{f.label}</Label>
          <Input id={f.name} type={f.type ?? 'text'} {...register(f.name)} />
          {errors[f.name] && (
            <span role="alert" className="text-sm text-red-600">{errors[f.name]?.message as string}</span>
          )}
        </div>
      ))}
      {errorMessage && <div role="alert" className="text-sm text-red-600">{errorMessage}</div>}
      <Button type="submit" disabled={isPending}>
        {isPending ? 'Registrando…' : 'Registrar organización'}
      </Button>
    </form>
  )
}
```

**`web/src/portal-admin/pages/LoginPage.tsx`:**
```tsx
import { Link } from 'react-router'
import { LoginForm } from '@/features/auth/components/LoginForm'
import { useLogin } from '@/features/auth/hooks/useLogin'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card'

export default function AdminLoginPage() {
  const mutation = useLogin()
  const errorMessage = mutation.isError ? 'Credenciales inválidas' : undefined

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 p-6">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>Ingreso Administrador</CardTitle>
        </CardHeader>
        <CardContent>
          <LoginForm
            onSubmit={(r) => mutation.mutate(r)}
            isPending={mutation.isPending}
            errorMessage={errorMessage}
            submitLabel="Ingresar"
          />
          <div className="mt-4 text-sm text-slate-600">
            ¿Nueva inmobiliaria? <Link to="/admin/register-org" className="text-blue-600 underline">Registrarse</Link>
          </div>
          <div className="mt-2 text-sm text-slate-600">
            ¿Inquilino? <Link to="/inquilino/login" className="text-blue-600 underline">Portal de inquilinos</Link>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
```

**`web/src/portal-admin/pages/RegisterOrgPage.tsx`:**
```tsx
import { Link } from 'react-router'
import { RegisterOrgForm } from '@/features/auth/components/RegisterOrgForm'
import { useRegisterOrg } from '@/features/auth/hooks/useRegisterOrg'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card'

export default function RegisterOrgPage() {
  const mutation = useRegisterOrg()
  const errorMessage = mutation.isError ? 'No se pudo registrar (slug en uso o datos inválidos)' : undefined

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 p-6">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>Registrar Organización</CardTitle>
        </CardHeader>
        <CardContent>
          <RegisterOrgForm
            onSubmit={(r) => mutation.mutate(r)}
            isPending={mutation.isPending}
            errorMessage={errorMessage}
          />
          <div className="mt-4 text-sm text-slate-600">
            ¿Ya tienes cuenta? <Link to="/admin/login" className="text-blue-600 underline">Ingresar</Link>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
```

**`web/src/portal-admin/pages/DashboardPage.tsx`:**
```tsx
import { useAuthStore } from '@/shared/stores/authStore'
import { Button } from '@/shared/components/ui/button'

export default function DashboardPage() {
  const user = useAuthStore((s) => s.user)
  const logout = useAuthStore((s) => s.logout)
  return (
    <div className="p-6">
      <h1 className="text-2xl font-semibold">Panel administrativo</h1>
      <p className="mt-2 text-slate-600">
        Bienvenido {user?.email} (rol: {user?.role}, org: {user?.organizationSlug})
      </p>
      <Button className="mt-4" variant="outline" onClick={logout}>Cerrar sesión</Button>
    </div>
  )
}
```

**`web/src/portal-admin/layouts/AdminLayout.tsx`:**
```tsx
import { Navigate, Outlet } from 'react-router'
import { useAuthStore } from '@/shared/stores/authStore'

export default function AdminLayout() {
  const user = useAuthStore((s) => s.user)
  if (!user || user.role === 'Tenant') {
    return <Navigate to="/admin/login" replace />
  }
  return <Outlet />
}
```

**`web/src/portal-admin/routes.tsx`:**
```tsx
import type { RouteObject } from 'react-router'
import AdminLayout from './layouts/AdminLayout'
import AdminLoginPage from './pages/LoginPage'
import RegisterOrgPage from './pages/RegisterOrgPage'
import DashboardPage from './pages/DashboardPage'

export const adminRoutes: RouteObject[] = [
  { path: 'login', element: <AdminLoginPage /> },
  { path: 'register-org', element: <RegisterOrgPage /> },
  {
    path: '',
    element: <AdminLayout />,
    children: [
      { path: 'dashboard', element: <DashboardPage /> },
      { index: true, element: <DashboardPage /> },
    ],
  },
]
```

**`web/src/portal-inquilino/pages/LoginPage.tsx`:**
```tsx
import { Link } from 'react-router'
import { LoginForm } from '@/features/auth/components/LoginForm'
import { useTenantLogin } from '@/features/auth/hooks/useTenantLogin'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card'

export default function TenantLoginPage() {
  const mutation = useTenantLogin()
  const errorMessage = mutation.isError ? 'Credenciales inválidas' : undefined
  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 p-6">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>Portal del Inquilino</CardTitle>
        </CardHeader>
        <CardContent>
          <LoginForm
            onSubmit={(r) => mutation.mutate(r)}
            isPending={mutation.isPending}
            errorMessage={errorMessage}
            submitLabel="Ingresar al portal"
          />
          <div className="mt-4 text-sm text-slate-600">
            ¿Administrador? <Link to="/admin/login" className="text-blue-600 underline">Portal admin</Link>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
```

**`web/src/portal-inquilino/layouts/InquilinoLayout.tsx`:**
```tsx
import { Navigate, Outlet } from 'react-router'
import { useAuthStore } from '@/shared/stores/authStore'

export default function InquilinoLayout() {
  const user = useAuthStore((s) => s.user)
  if (!user || user.role !== 'Tenant') {
    return <Navigate to="/inquilino/login" replace />
  }
  return <Outlet />
}
```

**`web/src/portal-inquilino/routes.tsx`:**
```tsx
import type { RouteObject } from 'react-router'
import InquilinoLayout from './layouts/InquilinoLayout'
import TenantLoginPage from './pages/LoginPage'

function InquilinoHome() {
  return <div className="p-6"><h1 className="text-2xl font-semibold">Portal del inquilino</h1><p className="text-slate-600">Contenido disponible en fases futuras.</p></div>
}

export const inquilinoRoutes: RouteObject[] = [
  { path: 'login', element: <TenantLoginPage /> },
  {
    path: '',
    element: <InquilinoLayout />,
    children: [{ index: true, element: <InquilinoHome /> }],
  },
]
```

**Replace `web/src/App.tsx`:**
```tsx
import { createBrowserRouter, Navigate, RouterProvider } from 'react-router'
import { QueryClientProvider } from '@tanstack/react-query'
import { queryClient } from '@/shared/lib/queryClient'
import { adminRoutes } from '@/portal-admin/routes'
import { inquilinoRoutes } from '@/portal-inquilino/routes'

const router = createBrowserRouter([
  { path: '/admin/*', children: adminRoutes },
  { path: '/inquilino/*', children: inquilinoRoutes },
  { path: '/', element: <Navigate to="/admin/login" replace /> },
  { path: '*', element: <Navigate to="/admin/login" replace /> },
])

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  )
}
```

**Replace `web/src/main.tsx`:**
```tsx
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
```

**`web/src/features/auth/__tests__/LoginForm.test.tsx`:**
```tsx
import { describe, it, expect, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { LoginForm } from '../components/LoginForm'

describe('LoginForm', () => {
  it('shows validation errors when submitting empty form', async () => {
    const onSubmit = vi.fn()
    render(<LoginForm onSubmit={onSubmit} isPending={false} submitLabel="Ingresar" />)
    const user = userEvent.setup()
    await user.click(screen.getByRole('button', { name: /ingresar/i }))
    await waitFor(() => {
      expect(screen.getAllByRole('alert').length).toBeGreaterThanOrEqual(1)
    })
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('calls onSubmit with typed values when form is valid', async () => {
    const onSubmit = vi.fn()
    render(<LoginForm onSubmit={onSubmit} isPending={false} submitLabel="Ingresar" />)
    const user = userEvent.setup()
    await user.type(screen.getByLabelText(/organización/i), 'acme')
    await user.type(screen.getByLabelText(/email/i), 'admin@acme.com')
    await user.type(screen.getByLabelText(/contraseña/i), 'SuperSecret123')
    await user.click(screen.getByRole('button', { name: /ingresar/i }))
    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith({
        organizationSlug: 'acme',
        email: 'admin@acme.com',
        password: 'SuperSecret123',
      })
    })
  })

  it('rejects invalid email format', async () => {
    const onSubmit = vi.fn()
    render(<LoginForm onSubmit={onSubmit} isPending={false} submitLabel="Ingresar" />)
    const user = userEvent.setup()
    await user.type(screen.getByLabelText(/organización/i), 'acme')
    await user.type(screen.getByLabelText(/email/i), 'not-an-email')
    await user.type(screen.getByLabelText(/contraseña/i), 'pass')
    await user.click(screen.getByRole('button', { name: /ingresar/i }))
    await waitFor(() => {
      expect(screen.getByText(/email inválido/i)).toBeInTheDocument()
    })
    expect(onSubmit).not.toHaveBeenCalled()
  })
})
```

Run `cd web && pnpm build && pnpm test --run`. Both must exit 0.
</action>
<acceptance_criteria>
- File exists: `web/src/features/auth/components/LoginForm.tsx` and contains `zodResolver` and `aria-label="login-form"`
- File exists: `web/src/features/auth/components/RegisterOrgForm.tsx` and contains `aria-label="register-org-form"`
- File exists: `web/src/portal-admin/pages/LoginPage.tsx` and contains `useLogin`
- File exists: `web/src/portal-admin/pages/RegisterOrgPage.tsx` and contains `useRegisterOrg`
- File exists: `web/src/portal-admin/pages/DashboardPage.tsx`
- File exists: `web/src/portal-admin/layouts/AdminLayout.tsx` and contains `role === 'Tenant'`
- File exists: `web/src/portal-admin/routes.tsx`
- File exists: `web/src/portal-inquilino/pages/LoginPage.tsx` and contains `useTenantLogin`
- File exists: `web/src/portal-inquilino/layouts/InquilinoLayout.tsx` and contains `role !== 'Tenant'`
- File exists: `web/src/portal-inquilino/routes.tsx`
- `web/src/App.tsx` contains `'/admin/*'` and `'/inquilino/*'` and `QueryClientProvider`
- File exists: `web/src/features/auth/__tests__/LoginForm.test.tsx`
- Grep `web/src/` recursively for `: any` (with TS type annotation syntax) returns ZERO matches (TS strict)
- Grep `web/src/` recursively for `@ts-ignore` returns ZERO matches
- `cd web && pnpm build` exits 0
- `cd web && pnpm test --run` exits 0 with at least 3 passing tests from LoginForm.test.tsx
</acceptance_criteria>
</task>

## Verification

- `cd web && pnpm install` exits 0
- `cd web && pnpm build` exits 0
- `cd web && pnpm test --run` exits 0
- File does NOT exist: `web/tailwind.config.js`, `web/tailwind.config.ts`, `web/postcss.config.js`, `web/postcss.config.cjs`
- Grep `web/src/` recursively for `useEffect.*fetch` returns ZERO matches (no useEffect-based fetching)
- Grep `web/src/features/auth/hooks/` for `useMutation` returns at least 3 matches (one per hook)

## Threat Model

| ID | Threat | Category | Mitigation | ASVS |
|----|--------|----------|------------|------|
| T-1-19 | JWT persisted in localStorage readable by XSS | Information Disclosure | Zustand `persist` uses localStorage key `gestion-alquileres-auth`; Phase 1 accepts this tradeoff (documented in research). Future phases should consider httpOnly cookie when refresh tokens are added. CSP headers and input sanitization in shadcn primitives reduce XSS risk | V3 |
| T-1-20 | JWT auto-sent to wrong origin | Information Disclosure | Axios `baseURL` pinned to `VITE_API_URL`; no wildcard origins; interceptor attaches Authorization only on `api` instance, not a global axios | V3 |
| T-1-21 | Admin JWT used to access tenant portal UI (or vice versa) | Elevation of Privilege | `AdminLayout` redirects `role === 'Tenant'` to `/admin/login`; `InquilinoLayout` redirects `role !== 'Tenant'` to `/inquilino/login`; server-side enforcement added in Plan 03 backend (tenant-login rejects Admin role) | V4 |
| T-1-22 | 401 response leaves stale token causing redirect loop | Availability | Axios response interceptor calls `logout()` BEFORE redirect; idempotent on already-unauthenticated paths (checked via `path !== target`) | V3 |
| T-1-23 | Untyped form input enabling mass-assignment to backend | Tampering | All form payloads validated client-side with zod schemas matching server FluentValidation rules (slug regex, email format, password length); server remains authoritative | V5 |
| T-1-24 | TypeScript `any` / `@ts-ignore` hiding type errors | Tampering | TSConfig `strict: true`; CLAUDE.md rule enforced; grep check in verification | V5 |
