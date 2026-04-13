# AGENTS.md — Web (React 19 + TypeScript)

## Rol
Senior React engineer. Implementás interfaces para el portal admin de inmobiliarias
y el portal de inquilinos, con datos de alquileres argentinos (ICL/IPC).

## Stack
- React 19 + TypeScript 5 (strict)
- Vite 6
- TanStack Query v5 (server state)
- Zustand (client state, solo lo que no es server state)
- React Router v7
- Axios (con interceptors para JWT + tenant)
- React Hook Form + Zod (formularios)
- TailwindCSS 4 + shadcn/ui (componentes)
- Vitest + Testing Library (tests)

## Estructura de carpetas

```
src/
  features/
    contratos/
      components/       -- ContratoCard, ContratoTable, AjusteModal
      hooks/            -- useContratos, useCalcularAjuste
      services/         -- contratoService.ts (llamadas a la API)
      types/            -- Contrato, ContratoDto, CreateContratoForm
    indices/
    propiedades/
    inquilinos/
    transacciones/
    documentos/
  portal-admin/
    layouts/            -- AdminLayout, Sidebar, Header
    pages/              -- Dashboard, Contratos, Propiedades, etc.
    routes.tsx
  portal-inquilino/
    layouts/            -- InquilinoLayout (simplificado)
    pages/              -- MiEstadoCuenta, MisRecibos, SubirComprobante
    routes.tsx
  shared/
    components/         -- Button, Modal, DataTable, Badge, etc.
    hooks/              -- useAuth, useTenant
    lib/
      api.ts            -- Axios instance con interceptors
      queryClient.ts
    types/              -- types compartidos
  App.tsx
  main.tsx
```

---

## Patrón de Service (llamadas a API)

```typescript
// features/contratos/services/contratoService.ts
import { api } from '@/shared/lib/api';
import type { Contrato, CreateContratoRequest, ContratoDto } from '../types';

export const contratoService = {
  getAll: () =>
    api.get<ContratoDto[]>('/api/v1/contratos').then(r => r.data),

  getById: (id: string) =>
    api.get<ContratoDto>(`/api/v1/contratos/${id}`).then(r => r.data),

  create: (data: CreateContratoRequest) =>
    api.post<ContratoDto>('/api/v1/contratos', data).then(r => r.data),

  calcularAjuste: (contratoId: string, period: string) =>
    api.post<RentHistoryDto>(`/api/v1/contratos/${contratoId}/ajuste`, { period }).then(r => r.data),
};
```

## Patrón de Hook (TanStack Query)

```typescript
// features/contratos/hooks/useContratos.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { contratoService } from '../services/contratoService';

export const CONTRATOS_KEY = ['contratos'] as const;

export function useContratos() {
  return useQuery({
    queryKey: CONTRATOS_KEY,
    queryFn: contratoService.getAll,
  });
}

export function useContrato(id: string) {
  return useQuery({
    queryKey: [...CONTRATOS_KEY, id],
    queryFn: () => contratoService.getById(id),
    enabled: !!id,
  });
}

export function useCreateContrato() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: contratoService.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CONTRATOS_KEY });
    },
  });
}

export function useCalcularAjuste() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ contratoId, period }: { contratoId: string; period: string }) =>
      contratoService.calcularAjuste(contratoId, period),
    onSuccess: (_data, { contratoId }) => {
      queryClient.invalidateQueries({ queryKey: [...CONTRATOS_KEY, contratoId] });
      queryClient.invalidateQueries({ queryKey: ['transacciones'] });
    },
  });
}
```

## Patrón de Formulario (React Hook Form + Zod)

```typescript
// features/contratos/components/CreateContratoForm.tsx
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useCreateContrato } from '../hooks/useContratos';

const createContratoSchema = z.object({
  propertyId: z.string().uuid('Propiedad inválida'),
  tenantId: z.string().uuid('Inquilino inválido'),
  startDate: z.string().min(1, 'Requerido'),
  endDate: z.string().min(1, 'Requerido'),
  initialRentAmount: z.number().positive('Debe ser mayor a 0'),
  adjustmentType: z.enum(['ICL', 'IPC', 'Manual', 'None']),
  dayOfMonthDue: z.number().int().min(1).max(28),
});

type CreateContratoForm = z.infer<typeof createContratoSchema>;

export function CreateContratoForm({ onSuccess }: { onSuccess: () => void }) {
  const { mutate: createContrato, isPending, error } = useCreateContrato();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CreateContratoForm>({
    resolver: zodResolver(createContratoSchema),
  });

  const onSubmit = (data: CreateContratoForm) => {
    createContrato(data, { onSuccess });
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      {/* campos del formulario */}
    </form>
  );
}
```

## Patrón de Axios Instance (multi-tenant)

```typescript
// shared/lib/api.ts
import axios from 'axios';
import { useAuthStore } from '../stores/authStore';

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  // El OrganizationId va en el JWT, no en headers manuales
  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      useAuthStore.getState().logout();
    }
    return Promise.reject(error);
  }
);
```

## Portal Admin vs Portal Inquilino

| Aspecto | Portal Admin | Portal Inquilino |
|---------|-------------|-----------------|
| Rutas base | `/admin/*` | `/inquilino/*` |
| Auth | JWT con rol Admin/Staff | JWT con rol Tenant |
| Features | CRUD completo | Solo lectura + subir comprobante |
| Documentos | Todos (IsVisibleToTenant: any) | Solo IsVisibleToTenant = true |
| Balance | Ver todos los inquilinos | Solo el propio |
| Ajustes | Puede calcular/editar | Solo ver resultado |

## Naming Conventions

| Qué | Patrón | Ejemplo |
|-----|--------|---------|
| Componentes | PascalCase | `ContratoCard.tsx` |
| Hooks | `use` + PascalCase | `useContratos.ts` |
| Services | camelCase + Service | `contratoService.ts` |
| Query Keys | UPPER_SNAKE_CASE const | `CONTRATOS_KEY` |
| Schemas Zod | camelCase + Schema | `createContratoSchema` |
| Types/interfaces | PascalCase | `ContratoDto`, `CreateContratoForm` |
| Pages | PascalCase + Page | `ContratosPage.tsx` |
| Stores Zustand | camelCase + Store | `authStore.ts` |

## Reglas Críticas

1. **NUNCA** `useEffect` para fetching de datos — siempre TanStack Query.
2. **NUNCA** guardar server state en Zustand — Zustand solo para UI state (tema, sidebar, modal abierto).
3. **NUNCA** mostrar URL de documento directamente — siempre via endpoint API que retorna presigned URL.
4. **NUNCA** calcular/formatear pesos argentinos sin `Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' })`.
5. **SIEMPRE** tipar con TypeScript strict — no `any`, no `as unknown as X`.
6. **SIEMPRE** manejar estados `isLoading`, `isError` en componentes que usan queries.
7. **SIEMPRE** invalidar queries relacionadas en los `onSuccess` de mutations.

## Formateo de datos argentinos

```typescript
// shared/lib/formatters.ts

// Pesos argentinos
export const formatARS = (amount: number) =>
  new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(amount);
// → "$ 150.000,00"

// Porcentaje de ajuste
export const formatPct = (pct: number) =>
  new Intl.NumberFormat('es-AR', { style: 'percent', minimumFractionDigits: 2 }).format(pct / 100);
// → "12,50%"

// Fechas en formato argentino
export const formatDate = (date: string | Date) =>
  new Intl.DateTimeFormat('es-AR').format(new Date(date));
// → "15/3/2025"

// Período de índice (mes/año)
export const formatPeriod = (period: string) => {
  const [year, month] = period.split('-');
  const months = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];
  return `${months[parseInt(month) - 1]} ${year}`;
};
// → "Mar 2025"
```

## Forbidden Patterns

```typescript
// MAL — useEffect para fetching
useEffect(() => {
  fetch('/api/contratos').then(r => r.json()).then(setContratos);
}, []);

// BIEN — TanStack Query
const { data: contratos } = useContratos();

// MAL — Guardar server data en Zustand
const useContratoStore = create(() => ({ contratos: [] }));
// ... luego fetch y setState

// BIEN — TanStack Query maneja el cache
const { data: contratos } = useContratos();

// MAL — Mostrar importe sin formatear
<span>{contrato.currentRentAmount}</span>

// BIEN
<span>{formatARS(contrato.currentRentAmount)}</span>

// MAL — any en TypeScript
const handleData = (data: any) => { ... }

// BIEN — tipar correctamente
const handleData = (data: ContratoDto) => { ... }
```

## Checklist al Finalizar

- [ ] `pnpm lint` sin warnings
- [ ] `pnpm build` sin errores TypeScript
- [ ] No hay `any` en el código nuevo
- [ ] Importes monetarios formateados con `formatARS()`
- [ ] Estados de loading/error manejados en todos los componentes con queries
- [ ] Documentos accedidos via endpoint API (no URL directa)
- [ ] Portal admin y portal inquilino tienen rutas y layouts separados
