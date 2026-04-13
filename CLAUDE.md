# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

SaaS Multi-tenant de Gestión de Alquileres para el mercado argentino. Automatiza cálculos de
aumentos (ICL/IPC del BCRA/INDEC) y comunicación con inquilinos.

- **api/** — Backend .NET 8 Web API. Clean Architecture. PostgreSQL via EF Core.
- **web/** — Frontend React 19 + TypeScript + Vite. Portal admin + portal inquilinos.

Solution: `api/GestionAlquileres.sln`

## Build & Development Commands

### API (.NET 8)
```bash
cd api && dotnet build                                        # compilar
cd api && dotnet run --project src/GestionAlquileres.API     # dev server puerto 5000
cd api && dotnet test                                         # tests
cd api && dotnet ef database update --project src/GestionAlquileres.Infrastructure  # migraciones
cd api && dotnet ef migrations add [Nombre] --project src/GestionAlquileres.Infrastructure  # nueva migración
```

### Web (React 19 + Vite)
```bash
cd web && pnpm dev       # Vite dev server puerto 5173
cd web && pnpm build     # tsc && vite build
cd web && pnpm test      # vitest
cd web && pnpm lint      # eslint (zero warnings)
```

## Architecture

### API — Clean Architecture (4 capas)

- **Domain/** — Entidades, Value Objects, Domain Events, interfaces de repositorios.
  NUNCA referencias a EF Core, HTTP, ni infraestructura.
- **Application/** — CQRS con MediatR. Commands, Queries, Handlers, Validators (FluentValidation).
  NUNCA acceso directo a DbContext ni HttpClient.
- **Infrastructure/** — EF Core (DbContext, Repositorios), BCRA/INDEC API clients, storage (MinIO/Azure).
  Implementa interfaces definidas en Domain.
- **API/** — Controllers HTTP. Solo: validar JWT, extraer OrganizationId del token, llamar MediatR, retornar resultado.
  NUNCA business logic en controllers.

### Multi-tenancy
- **Discriminador:** `OrganizationId` (Guid) en todas las entidades con datos de tenant.
- **Filtro global EF Core:** `HasQueryFilter(e => e.OrganizationId == _currentTenant.OrganizationId)` en todas las entidades multi-tenant.
- **OrganizationId** se extrae del JWT en cada request. NUNCA aceptar OrganizationId del body del request.

### Cálculo de Índices (lógica crítica)
- **ICL** (Índice de Contratos de Locación): ajuste trimestral. Fórmula: `NuevoAlquiler = AlquilerActual × (ICL_T / ICL_T-4)`
- **IPC** (Índice de Precios al Consumidor): acumulación mensual según período contractual.
- Los valores de índices se persisten en tabla `Indexes` ANTES de calcular — nunca calcular on-the-fly sin dato persistido.
- Si la API del BCRA falla, usar último valor disponible en DB y loguear warning.

### Web — Feature-based
- **features/{nombre}/** — Components, hooks, types, services por feature.
- **shared/** — UI components reutilizables, API client (axios con interceptors).
- **portal-admin/** — Rutas y layouts para administradores.
- **portal-inquilino/** — Rutas y layouts para inquilinos (auth separada).

## Naming Conventions

### Backend (.NET 8)
- Clases: PascalCase (`ContratoService`, `RentHistoryQuery`)
- Interfaces: prefijo `I` (`IContratoRepository`, `IIndexService`)
- Commands/Queries: `{Accion}{Recurso}Command` / `{Accion}{Recurso}Query`
- Handlers: `{Accion}{Recurso}CommandHandler` / `{Accion}{Recurso}QueryHandler`
- Entidades DB: singular PascalCase (`Organization`, `Contract`, `RentHistory`)
- Tablas DB: snake_case plural (`organizations`, `contracts`, `rent_history`) — configurar en EF
- DTOs: `{Recurso}Dto`, `Create{Recurso}Request`, `Update{Recurso}Request`

### Frontend (React 19 + TS)
- Componentes: PascalCase (`ContratoCard.tsx`)
- Hooks: `use` prefix (`useContratos`, `useCalculoAjuste`)
- Servicios API: `{recurso}Service.ts` (`contratoService.ts`)
- Rutas: kebab-case (`/contratos/:id/ajuste`)

## Critical Rules

- **Multi-tenancy siempre** — Todo query EF Core debe ir por el filtro global. NUNCA `IgnoreQueryFilters()` en producción.
- **OrganizationId del JWT** — NUNCA del body del request. Extraer en BaseController o middleware.
- **Índices persistidos** — NUNCA calcular ajustes con valores traídos en el momento de la API BCRA. Primero persistir, luego calcular.
- **Documentos privados** — NUNCA URLs directas al storage. Siempre URLs pre-firmadas de corta duración (5 min) generadas por la API con verificación de permisos.
- **EF Core** — NUNCA SQL raw (`FromSqlRaw`) salvo casos extremos documentados. Siempre LINQ + repositorios.
- **Ajuste Manual** — Siempre registrar en `RentHistory` con `AdjustmentType = Manual` y campo `Notes` obligatorio.
- **TypeScript strict** — no `any`, no `@ts-ignore` en el frontend.

## Business Logic: Flujo de Ajuste de Alquiler

```
1. Scheduler mensual dispara AjusteProgramadoCommand para contratos con fecha de ajuste = hoy
2. Buscar índice correspondiente al período en tabla Indexes
3. Si no existe → disparar SyncIndexCommand (BCRA/INDEC), luego reintentar
4. Calcular nuevo importe según fórmula del AdjustmentType del contrato
5. Crear registro en RentHistory con montos y referencia al índice usado
6. Crear Transaction de tipo RentCharge con el nuevo importe
7. Si contrato tiene email de inquilino → enviar notificación con detalle del ajuste
8. Si hay Ajuste Manual (descuento/punitorio) → crear Transaction adicional con tipo Manual
```

## Per-App AGENTS.md Files

- `api/AGENTS.md` — Patrones Clean Architecture .NET 8, EF Core, CQRS, multi-tenancy
- `web/AGENTS.md` — Patrones React 19, hooks, portal admin vs inquilino
