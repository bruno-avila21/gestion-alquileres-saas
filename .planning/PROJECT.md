# Gestión Alquileres SaaS

## What This Is

SaaS Multi-tenant de Gestión de Alquileres para el mercado argentino. Permite a inmobiliarias
y administradores de propiedades automatizar el cálculo de aumentos por índices ICL/IPC (BCRA/INDEC),
gestionar contratos, y proveer a inquilinos un portal self-service para consultar su estado de cuenta
y subir comprobantes de pago.

## Core Value

El cálculo automático de ajustes ICL/IPC con datos persistidos del BCRA debe ser 100% confiable
y auditable — si falla cualquier otra cosa, este cálculo no puede fallar.

## Requirements

### Validated

(None yet — ship to validate)

### Active

**Multi-tenancy e Infraestructura**
- [ ] Arquitectura multi-tenant con OrganizationId como discriminador (shared DB, PostgreSQL)
- [ ] JWT con OrganizationId embebido; global query filters en EF Core
- [ ] Clean Architecture .NET 8: Domain / Application / Infrastructure / API

**Gestión de Índices (BCRA/INDEC)**
- [ ] Servicio que consume API del BCRA para obtener valores ICL e IPC
- [ ] Persistencia local de índices en tabla `Indexes` antes de cualquier cálculo
- [ ] Fallback a último valor disponible en DB si la API del BCRA falla
- [ ] Soporte para ICL (trimestral) e IPC (mensual/trimestral/anual)

**Gestión de Contratos**
- [ ] CRUD de Organizations, Properties, Tenants (perfiles de inquilino), Contracts
- [ ] Contrato vincula: Inquilino + Propiedad + Inmobiliaria (Organization)
- [ ] AdjustmentType: ICL, IPC, Manual, None
- [ ] AdjustmentFrequency: Monthly, Quarterly, Annual
- [ ] Cálculo automático de ajuste con fórmula correcta por tipo
- [ ] Ajuste Manual: descuentos o punitorios extraordinarios (Notes obligatorio)

**Historial y Transacciones**
- [ ] RentHistory: registro de cada ajuste con referencia al índice usado
- [ ] Transactions: cargos de alquiler, pagos, descuentos, punitorios
- [ ] Balance por contrato consultable para admin e inquilino

**Gestión de Documentos**
- [ ] Upload de documentos (recibos, contratos, comprobantes) por admin
- [ ] Flag `IsVisibleToTenant` por documento (default: false)
- [ ] Documentos privados por defecto — acceso via presigned URLs (5 min)
- [ ] Inquilino puede subir fotos de comprobantes de pago

**Portal del Inquilino**
- [ ] Auth separada para inquilinos (JWT con rol Tenant)
- [ ] Ver estado de cuenta actual (balance, próximo vencimiento)
- [ ] Descargar recibos/documentos donde IsVisibleToTenant = true
- [ ] Subir comprobantes de pago (fotos/PDFs)

**Jobs Programados**
- [ ] Scheduler mensual (Hangfire) para disparar ajustes automáticos
- [ ] Notificación a inquilinos cuando se aplica un ajuste

### Out of Scope

- Mobile app nativa — web-first, puede ser PWA más adelante
- OAuth/SSO (Google, Microsoft) — email/password suficiente para v1
- Multi-currency en v1 — foco en ARS; USD deferred a v2
- Firma electrónica de contratos — integración DocuSign/FirmaEN defer a v2
- Contabilidad/facturación electrónica AFIP — no en scope

## Context

- Mercado objetivo: Argentina. Los índices ICL e IPC son legalmente mandatorios para
  contratos de locación residencial (Ley 27.551 modificada por DNU 70/2023 y Ley 27.737).
- La ley cambió múltiples veces el índice de ajuste: primero IPC, luego ICL, luego a elección.
  El sistema debe soportar múltiples tipos por contrato.
- Punto crítico: los índices del BCRA tienen latencia de publicación — hay que persistirlos
  apenas estén disponibles para no bloquear el cálculo de ajustes.
- Stack definido: .NET 8 + React 19 + PostgreSQL + pnpm.

## Constraints

- **Tech Stack**: .NET 8, React 19 + Vite, PostgreSQL, EF Core 8, MediatR — sin cambios al stack base
- **Multi-tenancy**: Shared DB con discriminador, no DB-per-tenant — simplifica ops pero requiere global filters impecables
- **Documentos**: Nunca URLs directas al storage — siempre presigned URLs
- **OrganizationId**: Nunca del request body — siempre del JWT

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Clean Architecture 4 capas | Testabilidad y separación clara de lógica de negocio de infraestructura | — Pending |
| Shared DB con discriminador | Menor complejidad operacional vs DB-per-tenant para MVP | — Pending |
| Persistir índices BCRA localmente | Evitar dependencia de API externa en cálculos críticos y permitir cálculos históricos | — Pending |
| Presigned URLs para documentos | Seguridad: documentos privados sin exponer storage ni paths internos | — Pending |
| Hangfire para jobs | Persistencia de jobs en DB, retry automático, dashboard de monitoreo | — Pending |

---
*Last updated: 2026-04-12 after project initialization*
