---
phase: 01-scaffolding-multi-tenancy-foundation
plan: 02
subsystem: domain-infra
tags: [dotnet8, efcore, multitenancy, domain-entities, migrations, xunit, clean-architecture]

# Dependency graph
requires:
  - 01-solution-scaffold (solution structure, NuGet packages, Program.cs base)
provides:
  - Pure Domain entities: Organization, User, ITenantEntity marker, UserRole enum
  - ICurrentTenant abstraction (Guid.Empty for unauthenticated requests)
  - IOrganizationRepository + IUserRepository interfaces
  - AppDbContext with HasQueryFilter on User (OrganizationId == _currentTenant.OrganizationId)
  - Organization entity deliberately has NO global filter (it is the tenant root)
  - CurrentTenantService reading org_id JWT claim via IHttpContextAccessor
  - OrganizationRepository + UserRepository implementations
  - DependencyInjection.cs extension method AddInfrastructure
  - TenantMiddleware placeholder (extension point for future tenant-resolution strategies)
  - InitialCreate EF migration: organizations + users tables in snake_case
  - TenantIsolationTests: 3 proven tests (Tenant A cannot read Tenant B data)

affects: [03-auth-jwt, 04-cqrs-mediatr, all-subsequent-plans]

# Tech tracking
tech-stack:
  added:
    - Microsoft.EntityFrameworkCore.Design 8.0.11 (added to API.csproj for ef tools)
    - Microsoft.EntityFrameworkCore.InMemory 8.0.11 (test project)
    - Microsoft.AspNetCore.Mvc.Testing 8.0.11 (test project)
    - dotnet-ef global tool 8.0.11
  patterns:
    - HasQueryFilter closure over _currentTenant in AppDbContext.OnModelCreating
    - ITenantEntity marker interface pattern for future tenant entities
    - ICurrentTenant returns Guid.Empty (not throw) for unauthenticated requests
    - IgnoreQueryFilters() only in explicitly documented cross-tenant methods (login flow)
    - IEntityTypeConfiguration<T> classes in Persistence/Configurations/ via ApplyConfigurationsFromAssembly
    - DependencyInjection.cs extension method pattern for Infrastructure service registration

key-files:
  created:
    - api/src/GestionAlquileres.Domain/Enums/UserRole.cs
    - api/src/GestionAlquileres.Domain/Entities/ITenantEntity.cs
    - api/src/GestionAlquileres.Domain/Entities/Organization.cs
    - api/src/GestionAlquileres.Domain/Entities/User.cs
    - api/src/GestionAlquileres.Domain/Interfaces/Services/ICurrentTenant.cs
    - api/src/GestionAlquileres.Domain/Interfaces/Repositories/IOrganizationRepository.cs
    - api/src/GestionAlquileres.Domain/Interfaces/Repositories/IUserRepository.cs
    - api/src/GestionAlquileres.Infrastructure/Persistence/AppDbContext.cs
    - api/src/GestionAlquileres.Infrastructure/Persistence/Configurations/OrganizationConfiguration.cs
    - api/src/GestionAlquileres.Infrastructure/Persistence/Configurations/UserConfiguration.cs
    - api/src/GestionAlquileres.Infrastructure/Services/CurrentTenantService.cs
    - api/src/GestionAlquileres.Infrastructure/Persistence/Repositories/OrganizationRepository.cs
    - api/src/GestionAlquileres.Infrastructure/Persistence/Repositories/UserRepository.cs
    - api/src/GestionAlquileres.Infrastructure/DependencyInjection.cs
    - api/src/GestionAlquileres.API/Middleware/TenantMiddleware.cs
    - api/src/GestionAlquileres.Infrastructure/Persistence/Migrations/20260413022934_InitialCreate.cs
    - api/src/GestionAlquileres.Infrastructure/Persistence/Migrations/20260413022934_InitialCreate.Designer.cs
    - api/src/GestionAlquileres.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs
    - api/tests/GestionAlquileres.Tests/TenantIsolationTests.cs
  modified:
    - api/src/GestionAlquileres.API/Program.cs (AddInfrastructure + UseMiddleware<TenantMiddleware>)
    - api/src/GestionAlquileres.API/GestionAlquileres.API.csproj (added EF Design package)
    - api/tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj (added InMemory + Mvc.Testing)

key-decisions:
  - "HasQueryFilter on User only (not Organization): Organization IS the tenant root — filtering it by OrganizationId would be circular and always return empty"
  - "ICurrentTenant returns Guid.Empty (not throw) for unauthenticated: allows filter to match nothing safely; caller endpoints that require tenant enforce [Authorize] attribute (Plan 03)"
  - "IgnoreQueryFilters() in OrganizationRepository: defensive coding, Organization has no filter but intent is explicit — these methods cross tenants by design"
  - "GetByEmailAcrossOrgsAsync uses IgnoreQueryFilters: login flow needs user lookup before JWT exists"
  - "EF Design package added to API.csproj: required by dotnet-ef startup project discovery"

# Metrics
duration: ~5min
completed: 2026-04-13
---

# Phase 1 Plan 02: Domain Entities + EF Core Multi-Tenancy + Initial Migration Summary

**Pure Domain entities (Organization, User, ITenantEntity), AppDbContext with HasQueryFilter global tenant isolation, CurrentTenantService reading org_id JWT claim, EF Core InitialCreate migration creating snake_case tables, and 3 TenantIsolationTests proving Tenant A cannot read Tenant B data**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-04-13T02:26:12Z
- **Completed:** 2026-04-13T02:31:14Z
- **Tasks:** 3
- **Files modified:** 22

## Accomplishments

- Created Domain project entities, interfaces, and enums with zero infrastructure references (no EF Core, no HttpContext)
- Implemented AppDbContext with EF Core global query filter ensuring complete tenant isolation via `OrganizationId == _currentTenant.OrganizationId`
- Organization entity intentionally has NO global filter — it is the tenant root, not a tenant-scoped entity
- CurrentTenantService returns `Guid.Empty` (not exception) for unauthenticated requests — filter silently returns empty set
- Generated `InitialCreate` EF Core migration with snake_case table (`organizations`, `users`) and column (`organization_id`) names
- 3 TenantIsolationTests confirm: Tenant A isolated from Tenant B, unauthenticated gets empty set, Organization has no query filter

## Task Commits

1. **Task 1: Domain entities + enums + interfaces** - `8896092` (feat)
2. **Task 2: AppDbContext + EF configs + CurrentTenantService + DI wiring** - `d9e289a` (feat)
3. **Task 3: Initial EF migration + tenant isolation tests** - `c7a49ca` (feat)

## Files Created/Modified

**Domain (zero external deps):**
- `api/src/GestionAlquileres.Domain/Enums/UserRole.cs` — Admin=1, Staff=2, Tenant=3
- `api/src/GestionAlquileres.Domain/Entities/ITenantEntity.cs` — Marker interface
- `api/src/GestionAlquileres.Domain/Entities/Organization.cs` — Tenant root entity
- `api/src/GestionAlquileres.Domain/Entities/User.cs` — Implements ITenantEntity
- `api/src/GestionAlquileres.Domain/Interfaces/Services/ICurrentTenant.cs` — Guid.Empty contract
- `api/src/GestionAlquileres.Domain/Interfaces/Repositories/IOrganizationRepository.cs`
- `api/src/GestionAlquileres.Domain/Interfaces/Repositories/IUserRepository.cs`

**Infrastructure:**
- `api/src/GestionAlquileres.Infrastructure/Persistence/AppDbContext.cs` — HasQueryFilter on User
- `api/src/GestionAlquileres.Infrastructure/Persistence/Configurations/OrganizationConfiguration.cs`
- `api/src/GestionAlquileres.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- `api/src/GestionAlquileres.Infrastructure/Services/CurrentTenantService.cs`
- `api/src/GestionAlquileres.Infrastructure/Persistence/Repositories/OrganizationRepository.cs`
- `api/src/GestionAlquileres.Infrastructure/Persistence/Repositories/UserRepository.cs`
- `api/src/GestionAlquileres.Infrastructure/DependencyInjection.cs`
- `api/src/GestionAlquileres.Infrastructure/Persistence/Migrations/20260413022934_InitialCreate.cs`
- `api/src/GestionAlquileres.Infrastructure/Persistence/Migrations/20260413022934_InitialCreate.Designer.cs`
- `api/src/GestionAlquileres.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`

**API:**
- `api/src/GestionAlquileres.API/Middleware/TenantMiddleware.cs` — Placeholder extension point
- `api/src/GestionAlquileres.API/Program.cs` — Added AddInfrastructure + UseMiddleware<TenantMiddleware>
- `api/src/GestionAlquileres.API/GestionAlquileres.API.csproj` — Added EF Design package

**Tests:**
- `api/tests/GestionAlquileres.Tests/TenantIsolationTests.cs` — 3 passing tests
- `api/tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj` — Added InMemory + Mvc.Testing

## Decisions Made

- **HasQueryFilter on User only:** Organization is the tenant root. Filtering Organization by its own PK-derived field would be circular and would break cross-tenant auth flows. All future tenant entities (Property, Tenant, Contract) will follow the User pattern — add to `OnModelCreating` in AppDbContext.
- **ICurrentTenant returns Guid.Empty:** Makes filter match nothing for unauthenticated requests instead of throwing exceptions. All tenant endpoints will require `[Authorize]` (Plan 03) which enforces the 401 before the query is reached.
- **IgnoreQueryFilters() documented use cases:** Only in OrganizationRepository (Organization has no filter anyway) and `GetByEmailAcrossOrgsAsync` (login flow before JWT exists). Grep for `IgnoreQueryFilters` is in code review checklist.
- **EF Design added to API.csproj:** Required for `dotnet ef` startup project discovery. Uses `PrivateAssets=all` so it doesn't become a transitive dependency.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Microsoft.EntityFrameworkCore.Design missing from API.csproj**
- **Found during:** Task 3 (dotnet ef migrations add)
- **Issue:** `Your startup project 'GestionAlquileres.API' doesn't reference Microsoft.EntityFrameworkCore.Design`
- **Fix:** Added `Microsoft.EntityFrameworkCore.Design 8.0.11` with `PrivateAssets=all` to API.csproj
- **Files modified:** `api/src/GestionAlquileres.API/GestionAlquileres.API.csproj`
- **Commit:** `c7a49ca`

### Non-Issue Notes

- **docker compose up -d postgres failed:** Docker Desktop is not running in this environment. This is expected — migration files are committed and `dotnet ef database update` must be run once Docker is started. This is documented as a setup step and does not block plan completion.

---

**Total deviations:** 1 auto-fixed (Rule 3 - Blocking)
**Impact on plan:** EF Design package is a standard dev-time requirement for EF migrations and was correctly omitted from Infrastructure.csproj (which already has it). Adding it to API.csproj is a standard .NET EF tooling requirement.

## Issues Encountered

- Docker Desktop not running — `database update` skipped. Must run `docker compose up -d postgres && cd api && dotnet ef database update --project src/GestionAlquileres.Infrastructure --startup-project src/GestionAlquileres.API` to apply migrations once Docker is available.

## User Setup Required

Run once Docker is available:
```bash
docker compose up -d postgres
cd api && dotnet ef database update --project src/GestionAlquileres.Infrastructure --startup-project src/GestionAlquileres.API
```

## Next Phase Readiness

- Tenant isolation foundation complete — all subsequent entities (Property, Tenant, Contract, etc.) can implement `ITenantEntity` and add `HasQueryFilter` in AppDbContext
- DbContext, repositories, and ICurrentTenant fully wired
- JWT auth (Plan 03) can now embed `org_id` claim that CurrentTenantService will read
- MediatR CQRS (Plan 04) can use IOrganizationRepository and IUserRepository via DI

## Self-Check: PASSED

Files verified present:
- `api/src/GestionAlquileres.Domain/Entities/Organization.cs` - FOUND
- `api/src/GestionAlquileres.Domain/Entities/User.cs` - FOUND
- `api/src/GestionAlquileres.Infrastructure/Persistence/AppDbContext.cs` - FOUND
- `api/src/GestionAlquileres.Infrastructure/Persistence/Migrations/20260413022934_InitialCreate.cs` - FOUND
- `api/tests/GestionAlquileres.Tests/TenantIsolationTests.cs` - FOUND

Commits verified:
- `8896092` feat(1-02): create Domain entities, enums, interfaces (zero infra deps)
- `d9e289a` feat(1-02): AppDbContext + EF configs + CurrentTenantService + DI wiring
- `c7a49ca` feat(1-02): initial EF migration + tenant isolation integration tests

Tests: 4/4 passing (dotnet test exits 0) — 1 SmokeTest + 3 TenantIsolationTests

---
*Phase: 01-scaffolding-multi-tenancy-foundation*
*Completed: 2026-04-13*
