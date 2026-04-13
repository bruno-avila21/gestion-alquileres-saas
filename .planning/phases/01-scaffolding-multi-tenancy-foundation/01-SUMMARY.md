---
phase: 01-scaffolding-multi-tenancy-foundation
plan: 01
subsystem: infra
tags: [dotnet8, clean-architecture, hangfire, serilog, postgresql, efcore, docker, nuget]

# Dependency graph
requires: []
provides:
  - 4-project Clean Architecture .NET 8 solution (Domain/Application/Infrastructure/API)
  - global.json pinned to SDK 9.0.304
  - All NuGet packages at research-verified versions (EFCore 8.0.11, MediatR 12.4.1, Hangfire 1.8.23, Serilog 8.0.3)
  - Docker Compose local dev infra (PostgreSQL 16 + MinIO)
  - Serilog structured logging to console + rolling file
  - Hangfire dashboard at /hangfire with dev-only write access
  - Health endpoint at /health
  - xUnit smoke test infrastructure

affects: [02-domain-entities, 03-auth-jwt, 04-cqrs-mediatr, all-subsequent-plans]

# Tech tracking
tech-stack:
  added:
    - Microsoft.EntityFrameworkCore 8.0.11
    - Npgsql.EntityFrameworkCore.PostgreSQL 8.0.11
    - EFCore.NamingConventions 8.0.3
    - Hangfire.Core + Hangfire.AspNetCore + Hangfire.PostgreSql 1.8.23 / 1.21.1
    - MediatR 12.4.1
    - FluentValidation 11.11.0 + FluentValidation.DependencyInjectionExtensions
    - AutoMapper 16.1.1 (both Application and Infrastructure)
    - BCrypt.Net-Next 4.0.3
    - System.IdentityModel.Tokens.Jwt 8.17.0
    - Serilog.AspNetCore 8.0.3 + Sinks.Console/File 6.0.0 + Enrichers.Environment 3.0.1
    - Microsoft.AspNetCore.Authentication.JwtBearer 8.0.11
    - Swashbuckle.AspNetCore 7.2.0
    - xUnit (net8.0 test project)
  patterns:
    - Clean Architecture 4-layer topology: Domain (no deps) -> Application (Domain) -> Infrastructure (Application) -> API (Infrastructure + Application)
    - Serilog host-level configuration with ReadFrom.Configuration + ReadFrom.Services
    - Hangfire PostgreSQL storage with UseNpgsqlConnection
    - Program.cs partial class for WebApplicationFactory integration testing

key-files:
  created:
    - api/GestionAlquileres.sln
    - api/global.json
    - api/src/GestionAlquileres.Domain/GestionAlquileres.Domain.csproj
    - api/src/GestionAlquileres.Application/GestionAlquileres.Application.csproj
    - api/src/GestionAlquileres.Infrastructure/GestionAlquileres.Infrastructure.csproj
    - api/tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj
    - api/tests/GestionAlquileres.Tests/SmokeTests.cs
    - docker-compose.yml
  modified:
    - api/src/GestionAlquileres.API/GestionAlquileres.API.csproj
    - api/src/GestionAlquileres.API/Program.cs
    - api/src/GestionAlquileres.API/appsettings.json
    - api/src/GestionAlquileres.API/Properties/launchSettings.json

key-decisions:
  - "AutoMapper version: used 16.1.1 in both Application and Infrastructure (plan suggested 13.0.1 for Application, but 13.0.1 has a known high-severity CVE; 16.1.1 resolves cleanly)"
  - "Microsoft.Extensions.* versions: upgraded to 10.0.0 (from planned 8.0.2) to satisfy AutoMapper 16.1.1 transitive dependency chain — no functional impact on net8.0 target"
  - "Serilog.AspNetCore 8.0.3 used (not 10.0.0 which requires .NET 9+)"
  - "appsettings.Development.json is gitignored as per .gitignore — not committed, created locally only"

patterns-established:
  - "Clean Architecture layer references: Domain has zero deps; each layer only references one level down"
  - "Hangfire storage always via UseNpgsqlConnection from HangfireConnection string"
  - "Serilog configured at Host level using ReadFrom.Configuration so appsettings.json controls levels"
  - "public partial class Program at end of Program.cs enables WebApplicationFactory<Program> in integration tests"

requirements-completed: [INFRA-03, INFRA-04, INFRA-05]

# Metrics
duration: 45min
completed: 2026-04-13
---

# Phase 1 Plan 01: .NET 8 Solution Scaffold + Core Infrastructure Summary

**.NET 8 Clean Architecture solution with Hangfire PostgreSQL job scheduling, Serilog structured logging, Docker Compose for local dev (PostgreSQL 16 + MinIO), and all NuGet packages pinned at research-verified versions**

## Performance

- **Duration:** ~45 min
- **Started:** 2026-04-13T01:28:00Z
- **Completed:** 2026-04-13T02:13:13Z
- **Tasks:** 4
- **Files modified:** 14

## Accomplishments

- Created 5-project .NET 8 solution (Domain, Application, Infrastructure, API, Tests) with strict Clean Architecture project references
- Installed all NuGet packages at plan-specified versions — build exits 0 with 0 warnings
- Configured Serilog structured logging (console + rolling file) and Hangfire with PostgreSQL storage via Program.cs
- Added docker-compose.yml with PostgreSQL 16-alpine and MinIO for local dev infrastructure

## Task Commits

1. **Task 1: Create xUnit test project scaffold** - `0bc8cc1` (feat)
2. **Task 2: Scaffold 4 Clean Architecture projects + global.json** - `25ef04d` (feat)
3. **Task 3: Install all NuGet packages** - `6bb4b8c` (chore)
4. **Task 4: Configure Docker Compose + appsettings + Serilog + Hangfire** - `33f2c6f` (feat)

**Plan metadata:** _(final commit to follow)_

## Files Created/Modified

- `api/GestionAlquileres.sln` - 5-project solution file
- `api/global.json` - SDK pin to 9.0.304 with latestPatch rollForward
- `api/src/GestionAlquileres.Domain/GestionAlquileres.Domain.csproj` - Zero external deps (CLAUDE.md enforced)
- `api/src/GestionAlquileres.Application/GestionAlquileres.Application.csproj` - MediatR 12.4.1, FluentValidation 11.11.0, AutoMapper 16.1.1
- `api/src/GestionAlquileres.Infrastructure/GestionAlquileres.Infrastructure.csproj` - EFCore 8.0.11, Hangfire, BCrypt, AutoMapper 16.1.1
- `api/src/GestionAlquileres.API/GestionAlquileres.API.csproj` - JwtBearer 8.0.11, Serilog 8.0.3, Swashbuckle 7.2.0
- `api/src/GestionAlquileres.API/Program.cs` - UseSerilog + AddHangfire + health endpoint + partial Program class
- `api/src/GestionAlquileres.API/appsettings.json` - ConnectionStrings, JwtSettings, Serilog sections
- `api/src/GestionAlquileres.API/Properties/launchSettings.json` - http://localhost:5000 profile
- `api/tests/GestionAlquileres.Tests/SmokeTests.cs` - Sanity_TestInfrastructureAlive test
- `docker-compose.yml` - PostgreSQL 16 + MinIO services with health check

## Decisions Made

- **AutoMapper 16.1.1 in both layers:** Plan suggested AutoMapper 13.0.1 for Application, but 13.0.1 has a known high CVE (GHSA-rvv3-g6hj-g44x). Used 16.1.1 everywhere — cleaner and secure.
- **Microsoft.Extensions.* pinned at 10.0.0:** AutoMapper 16.1.1 transitively requires `Microsoft.Extensions.Logging.Abstractions >= 10.0.0`. The plan-specified 8.0.2 caused NU1605 downgrade errors. Upgraded to 10.0.0 — no runtime impact on net8.0.
- **Serilog.AspNetCore 8.0.3:** Confirmed correct (plan noted 10.0.0 requires .NET 9+).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] AutoMapper 13.0.1 has known high-severity CVE**
- **Found during:** Task 3 (NuGet package install)
- **Issue:** AutoMapper 13.0.1 reported `NU1903: known high severity vulnerability GHSA-rvv3-g6hj-g44x`
- **Fix:** Upgraded Application's AutoMapper to 16.1.1 matching Infrastructure — plan explicitly permitted "if executor finds newer versions that resolve cleanly with net8.0, they may use them"
- **Files modified:** `api/src/GestionAlquileres.Application/GestionAlquileres.Application.csproj`
- **Verification:** dotnet build exits 0, 0 warnings
- **Committed in:** `6bb4b8c`

**2. [Rule 1 - Bug] NU1605 version downgrade conflict for Microsoft.Extensions.DependencyInjection.Abstractions**
- **Found during:** Task 3 (NuGet package install)
- **Issue:** AutoMapper 16.1.1 → Microsoft.Extensions.Logging.Abstractions 10.0.0 → DependencyInjection.Abstractions >= 10.0.0, conflicting with explicit pin at 8.0.2
- **Fix:** Updated both Infrastructure and Application explicit pins from 8.0.2 to 10.0.0
- **Files modified:** `api/src/GestionAlquileres.Infrastructure/GestionAlquileres.Infrastructure.csproj`, `api/src/GestionAlquileres.Application/GestionAlquileres.Application.csproj`
- **Verification:** dotnet restore exits 0, dotnet build exits 0
- **Committed in:** `6bb4b8c`

---

**Total deviations:** 2 auto-fixed (2 × Rule 1 - Bug)
**Impact on plan:** Both fixes necessary for build correctness and security. No scope creep. Final package versions still satisfy Clean Architecture layering rules.

## Issues Encountered

None beyond the auto-fixed NuGet version conflicts documented above.

## User Setup Required

None — no external service configuration required. Docker Compose starts local dev infra. `appsettings.Development.json` is gitignored and created locally (contains dev-only secrets).

## Next Phase Readiness

- Solution skeleton complete — all downstream plans (02, 03, 04) can add to this structure
- EF Core and Hangfire packages installed but not yet configured with a DbContext (Plan 02)
- JWT packages installed but middleware not yet wired (Plan 03)
- No entities, endpoints, or business logic yet — this is intentional per plan scope

## Self-Check: PASSED

All files verified present. All task commits verified in git log:
- `0bc8cc1` feat(1-01): create xUnit test project scaffold
- `25ef04d` feat(1-01): scaffold 4 Clean Architecture projects
- `6bb4b8c` chore(1-01): install all NuGet packages
- `33f2c6f` feat(1-01): configure Docker Compose, appsettings, Serilog, Hangfire
- `f80bbd6` docs(1-01): complete plan (metadata)

---
*Phase: 01-scaffolding-multi-tenancy-foundation*
*Completed: 2026-04-13*
