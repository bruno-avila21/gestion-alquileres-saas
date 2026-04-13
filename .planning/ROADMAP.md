# Roadmap: Gestión Alquileres SaaS

**Version:** v1.0
**Goal:** SaaS funcional con multi-tenancy, cálculo ICL/IPC, contratos, portal inquilino

---

## Phase 1 — Scaffolding & Multi-tenancy Foundation

**Goal:** Proyecto .NET 8 y React 19 corriendo. Multi-tenancy con OrganizationId funcional.
Auth JWT con roles. Base de datos inicializada.

**Requirements:** INFRA-01..05, ORG-01..05

**Tasks:**
- Crear solución .NET 8 con Clean Architecture (4 proyectos: Domain, Application, Infrastructure, API)
- Configurar PostgreSQL + EF Core 8 + Npgsql
- Entidades base: Organization, User, Role
- JWT Authentication con OrganizationId en claims
- Global query filters en AppDbContext
- ICurrentTenant + ITenantMiddleware
- Endpoints: POST /auth/login, POST /auth/register-org, POST /auth/tenant-login
- Crear proyecto React 19 + Vite + TypeScript strict
- Configurar TanStack Query, React Router v7, Zustand, shadcn/ui
- API client (Axios) con interceptores JWT
- Login page (Admin y Tenant)
- Hangfire configurado (jobs pendientes)
- Serilog estructurado

**Success Criteria:**
- Admin puede registrar organización y hacer login
- OrganizationId en JWT, global filter aisla datos por tenant
- `dotnet build` + `pnpm build` sin errores

**Plans:** 4 plans
- [x] 01-PLAN.md — .NET 8 Solution Scaffold + Core Infrastructure (Wave 1)
- [x] 02-PLAN.md — Domain Entities + EF Core Multi-tenancy + Initial Migration (Wave 2)
- [x] 03-PLAN.md — CQRS Pipeline + JWT Auth + Auth Endpoints (Wave 3)
- [x] 04-PLAN.md — React 19 + Vite + Frontend Foundation + Login Page (Wave 1, parallel to 01)

---

## Phase 2 — Gestión de Índices BCRA/INDEC

**Goal:** Índices ICL e IPC del BCRA/INDEC se sincronizan y persisten. Disponibles para consulta.

**Requirements:** IDX-01..06

**Depends on:** Phase 1

**Tasks:**
- Entidad `IndexValue` (Domain): IndexType, Period, Value, VariationPct, Source, FetchedAt
- `IIndexRepository` interface (Domain)
- `SyncIndexCommand` + handler (Application): llama API BCRA, persiste en DB
- `GetIndexByPeriodQuery` + handler (Application)
- `BcraApiClient` (Infrastructure): consume API pública BCRA para ICL
- `IndecApiClient` (Infrastructure): IPC si aplica
- Fallback: si API externa falla, log warning + usar último valor disponible
- `IndexesController`: GET /api/v1/indexes, POST /api/v1/indexes/sync
- Config EF Core para IndexValue (sin global filter — índices son globales)
- Frontend: página de Índices en portal admin (tabla de valores, botón sync manual)

**Success Criteria:**
- Sync manual de ICL desde panel admin funciona
- Valores persistidos en DB con período y fuente correctos
- Si API BCRA está caída, sistema usa valor anterior y loguea warning

**Plans:** 5 plans
- [ ] 02-01-PLAN.md — Domain Entity + EF Core Config + Repository + Migration (Wave 1)
- [ ] 02-02-PLAN.md — BCRA + INDEC HTTP Clients with Resilience (Wave 1, parallel to 02-01)
- [ ] 02-03-PLAN.md — Application CQRS: SyncIndexCommand + GetIndexByPeriodQuery (Wave 2)
- [ ] 02-04-PLAN.md — IndexesController + Integration Tests + Live API Verification (Wave 3)
- [ ] 02-05-PLAN.md — Frontend Indexes Feature (React): Table + Sync Dialog + Admin Route (Wave 3, parallel to 02-04)

---

## Phase 3 — Properties & Tenants

**Goal:** Admin puede gestionar propiedades e inquilinos de su organización.

**Requirements:** PROP-01..02, TNT-01..03

**Depends on:** Phase 1

**Tasks:**
- Entidad `Property` (Domain): OrganizationId, dirección, tipo, superficie
- Entidad `AppTenant` (Domain): OrganizationId, datos personales, DNI, UserId nullable
- Repos + Commands/Queries CRUD para Property y AppTenant
- EF Core configs + migrations
- Controllers: PropertiesController, TenantsController
- Frontend: CRUD Properties (tabla + formulario)
- Frontend: CRUD Tenants (tabla + formulario + botón "Invitar al portal")
- Flujo de invitación: crear User con rol Tenant, enviar email con link/password temporal

**Success Criteria:**
- Admin puede crear, editar, listar propiedades de su organización
- Admin puede crear inquilinos e invitarlos al portal
- Datos de otra organización NO visibles (global filter funciona)

---

## Phase 4 — Gestión de Contratos

**Goal:** CRUD completo de contratos. Vincula Property + Tenant + Organization.

**Requirements:** CTR-01..05

**Depends on:** Phase 3

**Tasks:**
- Entidad `Contract` (Domain): todos los campos según ERD
- Enums: AdjustmentType, AdjustmentFrequency, ContractStatus, Currency
- Commands: CreateContractCommand, UpdateContractCommand, TerminateContractCommand
- Queries: GetContractByIdQuery, ListContractsQuery (con filtros: status, tenant, property)
- Validators: fechas, importe > 0, dayOfMonth 1-28, AdjustmentType válido
- EF Core config + migration
- ContractsController con endpoints CRUD
- Frontend: lista de contratos con filtros y estado visual
- Frontend: formulario de creación/edición con selectors de Property y Tenant
- Frontend: badge de estado (Activo/Vencido/Terminado)

**Success Criteria:**
- Admin puede crear contrato con todos los campos
- Validaciones del backend rechazan contratos inválidos
- Lista de contratos muestra solo los del tenant (OrganizationId)

---

## Phase 5 — Cálculo de Ajustes + Transacciones

**Goal:** Cálculo ICL/IPC funcional con índices persistidos. Transacciones de cargos y pagos.

**Requirements:** CALC-01..05, TRX-01..04

**Depends on:** Phase 2, Phase 4

**Tasks:**
- Entidades `RentHistory`, `Transaction` (Domain)
- `CalcularAjusteCommand` + handler: lógica ICL (×(T/T-12)) e IPC (acumulación)
- Validación: índice debe existir en DB — si no, lanzar excepción con mensaje claro
- `CreateTransactionCommand` (RentCharge, Penalty, Discount, Payment, Manual)
- `GetBalanceQuery`: sum(cargos) - sum(pagos) por contrato
- EF Core configs + migrations
- `AjusteMensualJob` (Hangfire): recorre contratos activos con fecha de ajuste = hoy
- ContractsController: POST /contratos/{id}/ajuste (manual trigger)
- TransactionsController: CRUD + balance endpoint
- Frontend: detalle de contrato con RentHistory (tabla de ajustes históricos)
- Frontend: tabla de transacciones con balance actual
- Frontend: botón "Calcular Ajuste" con modal de confirmación mostrando nuevo importe

**Success Criteria:**
- Ajuste ICL calcula correctamente con índice persistido (no live)
- Si índice no existe → error claro, no crash silencioso
- Scheduler Hangfire procesa ajustes automáticamente
- Balance por contrato visible para admin

---

## Phase 6 — Gestión de Documentos

**Goal:** Admin sube documentos. Flag IsVisibleToTenant controla acceso. Presigned URLs.

**Requirements:** DOC-01..05

**Depends on:** Phase 4

**Tasks:**
- Entidad `Document` (Domain): OrganizationId, ContractId, StoragePath, IsVisibleToTenant, DocumentType
- `UploadDocumentCommand`: subir archivo a storage (MinIO o Azure Blob), persistir metadatos
- `GetDocumentPresignedUrlQuery`: generar URL firmada (5 min expiración)
- `ToggleVisibilityCommand`: cambiar IsVisibleToTenant
- `IStorageService` interface (Domain) + implementación (Infrastructure)
- Config MinIO local para desarrollo (Docker Compose)
- DocumentsController: POST /upload, GET /{id}/url, PATCH /{id}/visibility
- Frontend: sección de documentos en detalle de contrato
- Frontend: upload con drag-and-drop, lista con toggle de visibilidad
- Frontend: descarga via presigned URL (no URL directa)

**Success Criteria:**
- Documentos no accesibles sin JWT válido + pertenencia al tenant
- URLs directas de storage NO expuestas en ningún endpoint
- Toggle de visibilidad funciona y persiste

---

## Phase 7 — Portal del Inquilino

**Goal:** Inquilino tiene portal para ver su estado, descargar recibos y subir comprobantes.

**Requirements:** PRT-01..05

**Depends on:** Phase 5, Phase 6

**Tasks:**
- Auth separada para inquilinos (JWT con rol Tenant, claims: tenantId + contractId)
- Middleware verifica que el inquilino solo accede a SU contrato
- `TenantPortalController`: endpoints de solo lectura para inquilino
  - GET /portal/estado-cuenta → balance, próximo vencimiento, importe actual
  - GET /portal/documentos → solo IsVisibleToTenant = true
  - GET /portal/ajustes → historial de ajustes del contrato
  - POST /portal/comprobante → subir comprobante de pago
- Frontend `portal-inquilino/`: layout simplificado, separado del portal admin
- Pages: EstadoCuentaPage, MisRecibosPage, SubirComprobantePage
- Diseño accesible: datos claros, sin jerga técnica

**Success Criteria:**
- Inquilino solo ve SU estado de cuenta (no puede acceder a otros contratos)
- Documentos mostrados solo donde IsVisibleToTenant = true
- Puede subir comprobante de pago (foto/PDF)
- Admin JWT NO puede acceder a endpoints del portal inquilino y viceversa

---

## Phase 8 — Notificaciones

**Goal:** Emails automáticos a inquilinos cuando se aplica un ajuste o se habilita un documento.

**Requirements:** NOTF-01..02

**Depends on:** Phase 5, Phase 6

**Tasks:**
- `IEmailService` interface (Domain) + implementación SMTP (MailKit o SendGrid)
- Email template: ajuste aplicado (muestra: importe anterior, nuevo, índice, período)
- Email template: documento disponible para descarga
- Disparar email en `CalcularAjusteCommandHandler` si inquilino tiene email
- Disparar email en `ToggleVisibilityCommandHandler` si se activa visibilidad
- Config: SMTP settings en appsettings, sin hardcodear
- Frontend: indicador en admin de "email enviado" en historial de ajuste

**Success Criteria:**
- Email de ajuste llega con datos correctos (importe, índice, período)
- Email NO se envía si el inquilino no tiene email configurado (no crash)
- Config SMTP via appsettings, no hardcodeada

---

## Milestone v1.0 — Completion Criteria

- [ ] Organización puede registrarse y gestionar su cartera
- [ ] Índices ICL/IPC sincronizados y persistidos desde BCRA
- [ ] Contratos con ajuste ICL/IPC calculado automáticamente
- [ ] Portal inquilino funcional (estado de cuenta + documentos + comprobante)
- [ ] Documentos siempre accesibles via presigned URLs
- [ ] Multi-tenancy: datos completamente aislados por organización
- [ ] `dotnet build` + `pnpm build` limpios
- [ ] Tests E2E del flujo crítico: sincronizar índice → crear contrato → calcular ajuste → ver en portal inquilino

---
*Roadmap created: 2026-04-12*
*Current milestone: v1.0 — Phase 2 planned, ready to execute*
