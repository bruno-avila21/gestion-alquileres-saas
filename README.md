---
title: Gestión Alquileres SaaS
description: SaaS multi-tenant para gestión de alquileres — .NET 8 + React 19 + PostgreSQL
---

# Gestión Alquileres SaaS

SaaS multi-tenant para gestión de propiedades en alquiler. Automatiza cálculos de ajuste
por índices argentinos (ICL/IPC del BCRA/INDEC) y comunicación con inquilinos.

## Stack

| Capa | Tecnología | Versión |
|------|------------|---------|
| Backend | .NET 8 Web API, Clean Architecture | 8.0 |
| ORM | EF Core + PostgreSQL 16 | — |
| CQRS | MediatR + FluentValidation | — |
| Storage | MinIO (S3-compatible) | — |
| Frontend | React + TypeScript + Vite | 19 / 4 |
| Router | React Router | 7 |
| Server state | TanStack Query | 5 |
| Client state | Zustand | 5 |
| Formularios | React Hook Form + Zod | — |
| Infra dev | Docker Compose (postgres + minio) | — |

## Correr localmente

```bash
# 1. Infraestructura (PostgreSQL + MinIO)
docker compose up -d

# 2. API (.NET 8)
cd api
dotnet build
dotnet ef database update --project src/GestionAlquileres.Infrastructure
dotnet run --project src/GestionAlquileres.API    # http://localhost:5000

# 3. Web (React 19)
cd web
pnpm install
pnpm dev    # http://localhost:5173
```

## Variables de entorno

**API** — `api/src/GestionAlquileres.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=gestion_alquileres;Username=appuser;Password=devpassword"
  },
  "JwtSettings": {
    "SecretKey": "clave_jwt_larga_y_aleatoria",
    "Issuer": "gestion-alquileres"
  },
  "Minio": {
    "Endpoint": "localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin"
  }
}
```

**Web** — `web/.env`:
```env
VITE_API_URL=http://localhost:5000/api/v1
```

## Arquitectura

```
api/src/
├── GestionAlquileres.Domain/        # Entidades, interfaces, enums
├── GestionAlquileres.Application/   # CQRS: Commands, Queries, Handlers, Validators
├── GestionAlquileres.Infrastructure/ # EF Core, repositorios, BCRA/INDEC clients, MinIO
└── GestionAlquileres.API/           # Controllers (solo MediatR.Send), Middleware

web/src/
├── features/{nombre}/               # Components, hooks, services, types por feature
├── shared/                          # api.ts, queryClient, authStore, UI components
├── portal-admin/                    # Rutas y páginas del panel admin
└── portal-inquilino/                # Rutas y páginas del portal de inquilinos
```

## Multi-tenancy

- Discriminador: `OrganizationId` (Guid) en todas las entidades
- Se extrae del claim `org_id` del JWT en `BaseController` — nunca del request body
- EF Core filtra automáticamente por `OrganizationId` con `HasQueryFilter`

## Módulos

- **Contratos**: vigencia, monto, tipo de ajuste (ICL/IPC/Manual)
- **Inquilinos**: gestión de contactos y portales individuales
- **Propiedades**: inmuebles con documentos privados (URLs pre-firmadas MinIO)
- **Ajuste automático**: scheduler mensual, sincronización de índices BCRA/INDEC
- **Transacciones**: historial de pagos y ajustes

## Comandos útiles

```bash
# API — nueva migración
cd api && dotnet ef migrations add NombreMigracion --project src/GestionAlquileres.Infrastructure

# API — tests
cd api && dotnet test

# Web — build y lint
cd web && pnpm build && pnpm lint

# MinIO — consola web
# http://localhost:9001 (minioadmin / minioadmin)
```

## AGENTS.md por módulo

- `api/AGENTS.md` — Patrones Clean Architecture, CQRS, EF Core, multi-tenancy
- `web/AGENTS.md` — Patrones React 19, TanStack Query, Zustand, portales
