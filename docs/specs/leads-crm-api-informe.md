# Informe — Backend CRM de consultas (leads), bloque A3

Implementación del contrato de `docs/specs/leads-crm.md` en `api/`, siguiendo el patrón CQRS de
`Features/Listings/**` y el mecanismo público por slug de `PublicController` / `TenantMiddleware`.

## Archivos

### Domain
- `src/GestionAlquileres.Domain/Enums/LeadSource.cs` — `Website`, `Manual`.
- `src/GestionAlquileres.Domain/Enums/LeadStatus.cs` — `New, Contacted, Visit, Negotiation, Won, Lost` (orden = columnas del kanban).
- `src/GestionAlquileres.Domain/Entities/Lead.cs` — `ITenantEntity`. `ListingId`/`PropertyId` nullable,
  con `OnDelete(SetNull)` (ver configuración EF): borrar una publicación o propiedad no debe borrar
  el historial de consultas.
- `src/GestionAlquileres.Domain/Entities/LeadNote.cs` — `ITenantEntity`.
- `src/GestionAlquileres.Domain/Interfaces/Repositories/ILeadRepository.cs`.

### Infrastructure
- `Persistence/Configurations/LeadConfiguration.cs`, `LeadNoteConfiguration.cs` — tablas
  `leads`/`lead_notes`, índices `(OrganizationId, Status)` y `(OrganizationId, CreatedAt)` en leads,
  `LeadId` en notes.
- `Persistence/Repositories/LeadRepository.cs` — `GetByIdAsync` (con Listing/Property/Notes),
  `GetForEditAsync` (sin notas, para update/status/delete/add-note), `GetPagedAsync` (filtro por
  status + búsqueda case-insensitive por name/email/phone/título del listing, mismo patrón
  `ToLower().Contains()` que `DocumentRepository`), `GetSummaryAsync` (GroupBy en SQL).
- `Persistence/AppDbContext.cs` — `DbSet<Lead>`, `DbSet<LeadNote>` + `HasQueryFilter` por
  `OrganizationId` para ambas.
- `DependencyInjection.cs` — registro de `ILeadRepository`.
- Migración: `20260909000558_AddLeads` (aplicada a la base local `gestion_alquileres`).

### Application
- `Features/Leads/DTOs/LeadDtos.cs` — `LeadDto`, `LeadDetailDto`, `LeadNoteDto`,
  `LeadSummaryDto` (con las 6 claves del enum siempre presentes, aunque el conteo sea 0).
- `Features/Leads/Commands/` — `CreateLeadCommand` (Manual), `UpdateLeadCommand`,
  `ChangeLeadStatusCommand`, `AddLeadNoteCommand`, `DeleteLeadCommand`, cada uno con su handler.
- `Features/Leads/Validators/LeadCommandValidators.cs`.
- `Features/Leads/Queries/` — `GetLeadsPageQuery` (usa `PagedResult<T>`/`Paging.Normalize`, igual que
  Documents/RentHistory), `GetLeadByIdQuery`, `GetLeadSummaryQuery`.
- `Features/Public/Commands/CreatePublicLeadCommand.cs` — resuelve org por slug (igual que
  `SearchPublicListingsQuery`/`GetPublicListingQuery`), valida que el listing exista y esté
  `Published` dentro del tenant ya resuelto por `TenantMiddleware`, usa `ICurrentTenant.OrganizationId`
  para el `OrganizationId` del lead (nunca del body/slug).

### API
- `Contracts/LeadRequests.cs`, `Contracts/PublicLeadRequest.cs` (con el honeypot `Website`).
- `Controllers/LeadsController.cs` — `AdminControllerBase` (Admin/Staff), rutas `api/v1/leads`.
- `Controllers/PublicController.cs` — nuevo `POST /api/v1/public/{slug}/leads` con
  `[EnableRateLimiting("public-leads")]`; el honeypot se descarta en el controller (antes de tocar
  Mediator/DB) devolviendo 204 sin más.
- `Program.cs` — nueva policy `public-leads` (10 req/min por IP, mismo patrón fixed-window por IP que
  `auth`).

### Tests
- `tests/GestionAlquileres.Tests/Phase12/LeadsCrmTests.cs` — 7 tests (detalle abajo).

## Decisiones / desvíos del contrato

1. **Sin AutoMapper.** `api/AGENTS.md` (genérico) pide un profile de AutoMapper para entidades
   nuevas, pero `Features/Listings/**` (el modelo que la tarea pidió reusar) no usa AutoMapper —
   usa factory estáticos (`ListingDto.From`). Repliqué ese patrón (`LeadDto.From`,
   `LeadDetailDto.From`, `LeadNoteDto.From`) para no mezclar convenciones dentro del mismo módulo
   de publicaciones/leads.
2. **"Not found" en Update/Status/Notes/Delete → 409, no 404.** El contrato sólo pide 404 explícito
   en `GET /leads/{id}`. Para los demás verbos seguí la convención ya establecida en
   `Listings`/`Properties`/`AppTenants` (`BusinessException` → 409 vía `ExceptionMiddleware`), no
   introduje un patrón nuevo.
3. **`LostReason` obligatorio en Lost se valida en el `Validator` (FluentValidation → 400), no en el
   handler.** Inicialmente lo dupliqué en el handler (→ 409) pero el pipeline de MediatR
   (`ValidationBehavior`) corre antes que cualquier handler, así que esa rama era inalcanzable; la
   saqué del handler para no dejar código muerto. Un test lo cubre explícitamente.
4. **Búsqueda por texto** usa `.ToLower().Contains()` (igual que `DocumentRepository`), no
   `EF.Functions.ILike` — no había precedente de `ILike` en el codebase y esto es funcionalmente
   equivalente (case-insensitive) vía la traducción de Npgsql.
5. **Policy `public-leads`:** no pude probarla con un test de 429 porque `app.UseRateLimiter()` sólo
   se activa fuera de `Development` (`Program.cs`), y la suite de tests corre en Development — mismo
   motivo por el que no existe un test de 429 para la policy `auth` existente. Quedó implementada y
   aplicada al endpoint; no verificada end-to-end en la suite.

## Cómo probar

```bash
docker compose up -d postgres   # desde la raíz del repo
cd api
dotnet ef database update --project src/GestionAlquileres.Infrastructure --startup-project src/GestionAlquileres.API \
  --connection "Host=localhost;Port=5432;Database=gestion_alquileres;Username=appuser;Password=devpassword"
dotnet build -c Release
dotnet test -c Release --filter "Phase=Phase12"
dotnet test -c Release --filter "Phase=Phase11"
```

Nota: hay una instancia de `GestionAlquileres.API.exe` corriendo desde una sesión anterior (`dotnet
run`, puerto 5000) que bloquea la carpeta `bin/Debug`. No la toqué; usé `-c Release` para build/test/
migraciones y evitar el conflicto de archivos con esa instancia.

## Endpoints nuevos

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| POST | /api/v1/public/{slug}/leads | Alta de consulta pública (honeypot, listingId opcional) | Anónimo, rate-limit 10/min/IP |
| GET | /api/v1/leads | Listado paginado (status, search, page, pageSize) | JWT Admin/Staff |
| GET | /api/v1/leads/summary | Conteo por estado para el kanban | JWT Admin/Staff |
| GET | /api/v1/leads/{id} | Detalle con notas | JWT Admin/Staff |
| POST | /api/v1/leads | Carga manual (Source=Manual) | JWT Admin/Staff |
| PUT | /api/v1/leads/{id} | Editar datos de contacto/mensaje | JWT Admin/Staff |
| PATCH | /api/v1/leads/{id}/status | Cambiar estado (lostReason oblig. si Lost) | JWT Admin/Staff |
| POST | /api/v1/leads/{id}/notes | Agregar nota (actualiza LastContactAt) | JWT Admin/Staff |
| DELETE | /api/v1/leads/{id} | Eliminar | JWT Admin/Staff |
