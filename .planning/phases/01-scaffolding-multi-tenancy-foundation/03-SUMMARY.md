---
phase: 01-scaffolding-multi-tenancy-foundation
plan: 03
subsystem: auth-cqrs
tags: [dotnet8, jwt, mediatr, cqrs, fluentvalidation, bcrypt, integration-tests, clean-architecture]

# Dependency graph
requires:
  - 01-solution-scaffold (NuGet packages, Program.cs base, project structure)
  - 02-domain-entities (Organization, User, UserRole, IOrganizationRepository, IUserRepository, AppDbContext)
provides:
  - IJwtService interface in Domain (zero infra deps)
  - JwtSettings POCO bound from configuration (SectionName = "JwtSettings")
  - JwtService generating JWT with sub, email, org_id, role, jti claims
  - MediatR CQRS pipeline with LoggingBehavior + ValidationBehavior
  - FluentValidation wired via AddValidatorsFromAssembly
  - RegisterOrgCommand+Handler: atomic Organization+Admin User creation, BCrypt.HashPassword
  - LoginCommand+Handler: Admin/Staff login, rejects UserRole.Tenant
  - TenantLoginCommand+Handler: Tenant-only login, rejects non-Tenant roles
  - All three commands have FluentValidation validators
  - ExceptionMiddleware: ValidationException->400, UnauthorizedAccessException->401, InvalidOperationException->409
  - JWT Bearer authentication wired in Program.cs (ClockSkew=30s)
  - BaseController: [Authorize] base, OrganizationId from JWT org_id claim, CurrentUserId from sub
  - AuthController: [AllowAnonymous] POST /api/v1/auth/register-org /login /tenant-login
  - 8 AuthTests integration tests via WebApplicationFactory<Program> with InMemory EF

affects: [04-frontend-scaffold, all-subsequent-plans]

# Tech tracking
tech-stack:
  added:
    - BCrypt.Net-Next 4.0.3 (added to Application.csproj — handlers call BCrypt directly per plan)
    - System.IdentityModel.Tokens.Jwt 8.17.0 (added to Tests.csproj for JwtSecurityTokenHandler)
  patterns:
    - MediatR IPipelineBehavior open generic registration via cfg.AddOpenBehavior(typeof(T<,>))
    - ValidationBehavior throws FluentValidation.ValidationException caught by ExceptionMiddleware -> 400
    - ExceptionMiddleware registered FIRST in pipeline to catch all downstream exceptions
    - BaseController derives ISender lazily from HttpContext.RequestServices (avoids constructor injection)
    - AuthController [AllowAnonymous] overrides BaseController [Authorize] for all three endpoints
    - WebApplicationFactory test factory: replaces DbContextOptions<AppDbContext> with InMemory, removes Hangfire IHostedService registrations to prevent disposal hang

key-files:
  created:
    - api/src/GestionAlquileres.Domain/Interfaces/Services/IJwtService.cs
    - api/src/GestionAlquileres.Application/Common/Settings/JwtSettings.cs
    - api/src/GestionAlquileres.Application/Common/DTOs/AuthResponseDto.cs
    - api/src/GestionAlquileres.Application/Common/Behaviors/ValidationBehavior.cs
    - api/src/GestionAlquileres.Application/Common/Behaviors/LoggingBehavior.cs
    - api/src/GestionAlquileres.Application/DependencyInjection.cs
    - api/src/GestionAlquileres.Application/Features/Auth/Commands/RegisterOrgCommand.cs
    - api/src/GestionAlquileres.Application/Features/Auth/Commands/RegisterOrgCommandHandler.cs
    - api/src/GestionAlquileres.Application/Features/Auth/Commands/RegisterOrgCommandValidator.cs
    - api/src/GestionAlquileres.Application/Features/Auth/Commands/LoginCommand.cs
    - api/src/GestionAlquileres.Application/Features/Auth/Commands/LoginCommandHandler.cs
    - api/src/GestionAlquileres.Application/Features/Auth/Commands/LoginCommandValidator.cs
    - api/src/GestionAlquileres.Application/Features/Auth/Commands/TenantLoginCommand.cs
    - api/src/GestionAlquileres.Application/Features/Auth/Commands/TenantLoginCommandHandler.cs
    - api/src/GestionAlquileres.Application/Features/Auth/Commands/TenantLoginCommandValidator.cs
    - api/src/GestionAlquileres.Infrastructure/Services/JwtService.cs
    - api/src/GestionAlquileres.API/Controllers/BaseController.cs
    - api/src/GestionAlquileres.API/Controllers/AuthController.cs
    - api/src/GestionAlquileres.API/Middleware/ExceptionMiddleware.cs
    - api/tests/GestionAlquileres.Tests/AuthTests.cs
  modified:
    - api/src/GestionAlquileres.Application/GestionAlquileres.Application.csproj (BCrypt.Net-Next added)
    - api/src/GestionAlquileres.Infrastructure/DependencyInjection.cs (JwtSettings + IJwtService registered)
    - api/src/GestionAlquileres.API/Program.cs (JWT auth, ExceptionMiddleware, AddApplication)
    - api/tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj (System.IdentityModel.Tokens.Jwt added)

key-decisions:
  - "BCrypt in Application layer: Plan explicitly places BCrypt.HashPassword/Verify calls in Application handlers. Added BCrypt.Net-Next to Application.csproj to satisfy the build. CLAUDE.md forbids DbContext/HttpClient in Application — BCrypt is a utility with no infra coupling."
  - "ExceptionMiddleware first in pipeline: Must be registered before SerilogRequestLogging and all other middleware to catch exceptions from any downstream middleware or controller."
  - "Hangfire IHostedService removal in tests: WebApplicationFactory.Dispose calls StopAsync on all IHostedService registrations. Hangfire's BackgroundJobServerHostedService blocks for 30s waiting for in-flight jobs before cancelling. Test factory now removes all Hangfire IHostedService registrations to prevent 90-second test suite cleanup hang."
  - "AddAutoMapper(cfg => {}, assembly) signature: AutoMapper 16.x changed the DI extension API. The two-argument overload (config action + assembly) works across 12/13/16."
  - "org_id claim name matches CurrentTenantService: JwtService emits new(\"org_id\", ...) which exactly matches the claim name read by CurrentTenantService from Plan 02."

requirements-completed: [ORG-01, ORG-02, ORG-03, ORG-04, ORG-05]

# Metrics
duration: ~25min
completed: 2026-04-13
---

# Phase 1 Plan 03: CQRS Pipeline + JWT Auth + Auth Endpoints Summary

**MediatR CQRS pipeline with FluentValidation + Logging behaviors, JWT token generation/validation, three auth commands (RegisterOrg, Login, TenantLogin), AuthController at /api/v1/auth/*, and 8 passing integration tests proving JWT org_id claim, role-based endpoint segregation, and FluentValidation 400 responses**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-04-13T02:35:00Z
- **Completed:** 2026-04-13T03:00:00Z
- **Tasks:** 3
- **Files modified:** 24

## Accomplishments

- Wired MediatR CQRS pipeline with ValidationBehavior (FluentValidation -> 400) and LoggingBehavior (request/response logging)
- JwtService generates JWT with sub, email, org_id, role, jti claims; SecretKey validation enforced at startup (throws if <32 chars)
- RegisterOrgCommand atomically creates Organization + Admin User using shared DbContext (single SaveChangesAsync = implicit transaction)
- LoginCommand rejects UserRole.Tenant; TenantLoginCommand rejects non-Tenant — role segregation enforced at handler level
- ExceptionMiddleware converts FluentValidation.ValidationException -> 400, UnauthorizedAccessException -> 401, InvalidOperationException -> 409
- JWT Bearer authentication wired with ClockSkew=30s (well below default 5min)
- BaseController exposes OrganizationId strictly from JWT org_id claim — never from request body (CLAUDE.md enforced)
- 8/8 AuthTests pass via WebApplicationFactory<Program> with InMemory EF; all 12 total tests pass

## Task Commits

1. **Task 1: Define interfaces/DTOs/settings + MediatR pipeline behaviors + Application DI** - `f5cbe7f` (feat)
2. **Task 2: JwtService + Auth commands/handlers/validators + ExceptionMiddleware + JWT middleware** - `e07dba5` (feat)
3. **Task 3: BaseController + AuthController + integration tests** - `b5d379b` (feat)

## Files Created/Modified

**Domain (zero external deps):**
- `api/src/GestionAlquileres.Domain/Interfaces/Services/IJwtService.cs` — string GenerateToken(User user)

**Application:**
- `api/src/GestionAlquileres.Application/Common/Settings/JwtSettings.cs` — Issuer, Audience, SecretKey, ExpiryHours
- `api/src/GestionAlquileres.Application/Common/DTOs/AuthResponseDto.cs` — record with Token, UserId, Email, Role, OrganizationId, OrganizationSlug
- `api/src/GestionAlquileres.Application/Common/Behaviors/ValidationBehavior.cs` — throws ValidationException on failures
- `api/src/GestionAlquileres.Application/Common/Behaviors/LoggingBehavior.cs` — logs request name before/after
- `api/src/GestionAlquileres.Application/DependencyInjection.cs` — AddMediatR + AddValidatorsFromAssembly + AddAutoMapper
- `api/src/GestionAlquileres.Application/Features/Auth/Commands/` — 9 files: 3 commands + 3 handlers + 3 validators

**Infrastructure:**
- `api/src/GestionAlquileres.Infrastructure/Services/JwtService.cs` — JWT generation with org_id claim
- `api/src/GestionAlquileres.Infrastructure/DependencyInjection.cs` — added Configure<JwtSettings> + AddScoped<IJwtService>

**API:**
- `api/src/GestionAlquileres.API/Controllers/BaseController.cs` — [Authorize] base, OrganizationId from JWT
- `api/src/GestionAlquileres.API/Controllers/AuthController.cs` — [AllowAnonymous] /register-org /login /tenant-login
- `api/src/GestionAlquileres.API/Middleware/ExceptionMiddleware.cs` — centralized exception handling
- `api/src/GestionAlquileres.API/Program.cs` — AddApplication(), JWT auth, ExceptionMiddleware first

**Tests:**
- `api/tests/GestionAlquileres.Tests/AuthTests.cs` — 8 integration tests (all passing)

## Decisions Made

- **BCrypt in Application.csproj:** The plan places BCrypt calls in Application handlers. Added BCrypt.Net-Next to Application.csproj to satisfy the build. CLAUDE.md prohibits DbContext/HttpClient in Application — BCrypt is a pure utility with no infrastructure coupling, so this is acceptable.
- **ExceptionMiddleware order:** Must be first in the pipeline to catch exceptions from all downstream middleware and controllers. SerilogRequestLogging follows immediately after.
- **Hangfire hosted service removal in tests:** Hangfire's `BackgroundJobServerHostedService.StopAsync` blocks for ~30s waiting for in-flight jobs. The test factory now removes all Hangfire `IHostedService` registrations by checking `ServiceType == IHostedService && ImplementationType.Assembly contains "Hangfire"`.
- **AddAutoMapper(cfg => {}, assembly):** AutoMapper 16.x changed the DI extension method signatures. The two-argument overload works correctly across 12/13/16.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] AddAutoMapper(assembly) signature changed in AutoMapper 16.x**
- **Found during:** Task 1 (dotnet build)
- **Issue:** `AddAutoMapper(assembly)` — CS1503 argument type mismatch. AutoMapper 16.x expects `AddAutoMapper(Action<IMapperConfigurationExpression>, Assembly)` not `AddAutoMapper(Assembly)`.
- **Fix:** Changed to `AddAutoMapper(cfg => { }, assembly)` — the two-argument overload that works across AutoMapper 12/13/16
- **Files modified:** `api/src/GestionAlquileres.Application/DependencyInjection.cs`
- **Commit:** `f5cbe7f`

**2. [Rule 2 - Missing Critical] BCrypt.Net-Next missing from Application.csproj**
- **Found during:** Task 2 (plan puts BCrypt calls in Application handlers)
- **Issue:** Application handlers call `BCrypt.Net.BCrypt.HashPassword` and `BCrypt.Net.BCrypt.Verify` but Application.csproj did not reference BCrypt.Net-Next
- **Fix:** Added `<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />` to Application.csproj
- **Files modified:** `api/src/GestionAlquileres.Application/GestionAlquileres.Application.csproj`
- **Commit:** `e07dba5`

**3. [Rule 1 - Bug] Hangfire BackgroundJobServerHostedService blocks test cleanup for 90 seconds**
- **Found during:** Task 3 (dotnet test)
- **Issue:** WebApplicationFactory.Dispose calls StopAsync on all IHostedService registrations. Hangfire's server waits for in-flight job completion before cancelling (default 30s per test class × multiple test runs = 90s+ total). All 8 AuthTests showed "Test Class Cleanup Failure: TaskCanceledException".
- **Fix:** Test factory removes Hangfire IHostedService registrations by filtering `ServiceType == IHostedService && ImplementationType.Assembly.FullName contains "Hangfire"`
- **Files modified:** `api/tests/GestionAlquileres.Tests/AuthTests.cs`
- **Commit:** `b5d379b`

**4. [Rule 2 - Missing Critical] System.IdentityModel.Tokens.Jwt missing from test project**
- **Found during:** Task 3 (dotnet build after writing AuthTests.cs)
- **Issue:** AuthTests.cs uses `JwtSecurityTokenHandler` from `System.IdentityModel.Tokens.Jwt` but test project did not reference that package
- **Fix:** Added `<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.17.0" />` to GestionAlquileres.Tests.csproj
- **Files modified:** `api/tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj`
- **Commit:** `b5d379b`

**5. [Rule 2 - Missing Critical] Microsoft.Extensions.Configuration using missing from AuthTests.cs**
- **Found during:** Task 3 (dotnet build)
- **Issue:** `AddInMemoryCollection` extension method requires `using Microsoft.Extensions.Configuration;`
- **Fix:** Added the using directive to AuthTests.cs
- **Files modified:** `api/tests/GestionAlquileres.Tests/AuthTests.cs`
- **Commit:** `b5d379b`

---

**Total deviations:** 5 auto-fixed (2 × Rule 1 - Bug, 3 × Rule 2 - Missing Critical)
**Impact on plan:** All fixes required for build correctness and test reliability. No scope creep.

## Known Stubs

None — all auth endpoints are fully implemented and verified by integration tests.

## Threat Flags

No new threat surface beyond what the plan's threat model covers. All T-1-10 through T-1-18 mitigations are implemented:
- T-1-10: JwtService throws if SecretKey < 32 chars
- T-1-11: BCrypt.HashPassword / BCrypt.Verify — no MD5/SHA1
- T-1-12: TenantLoginCommandHandler rejects `Role != Tenant`
- T-1-13: LoginCommandHandler rejects `Role == Tenant`
- T-1-14: No Command record declares OrganizationId as input field
- T-1-15: All auth failures return identical "Invalid credentials." message
- T-1-16: Commands are record types with explicit positional parameters
- T-1-17: JwtService adds Jti claim to every token
- T-1-18: ClockSkew = TimeSpan.FromSeconds(30)

## Issues Encountered

- Docker Desktop not running — cannot test against real PostgreSQL. All tests use InMemory EF. Integration against PostgreSQL requires running `docker compose up -d postgres` first.

## User Setup Required

None for test execution. For running the API locally:
```bash
docker compose up -d postgres
cd api && dotnet ef database update --project src/GestionAlquileres.Infrastructure --startup-project src/GestionAlquileres.API
cd api && dotnet run --project src/GestionAlquileres.API
```

## Next Phase Readiness

- Auth endpoints fully functional — all downstream plans can hit /api/v1/auth/register-org and get a JWT
- BaseController provides OrganizationId accessor — all future controllers inherit this automatically
- MediatR CQRS pipeline ready — new features only need Command + Handler + Validator files
- FluentValidation wired — all future Commands get validation for free via AddValidatorsFromAssembly

## Self-Check

### Files Present
- `api/src/GestionAlquileres.Domain/Interfaces/Services/IJwtService.cs` - FOUND
- `api/src/GestionAlquileres.Application/Common/Settings/JwtSettings.cs` - FOUND
- `api/src/GestionAlquileres.Application/Common/DTOs/AuthResponseDto.cs` - FOUND
- `api/src/GestionAlquileres.Application/Common/Behaviors/ValidationBehavior.cs` - FOUND
- `api/src/GestionAlquileres.Application/Common/Behaviors/LoggingBehavior.cs` - FOUND
- `api/src/GestionAlquileres.Application/DependencyInjection.cs` - FOUND
- `api/src/GestionAlquileres.Infrastructure/Services/JwtService.cs` - FOUND
- `api/src/GestionAlquileres.API/Controllers/BaseController.cs` - FOUND
- `api/src/GestionAlquileres.API/Controllers/AuthController.cs` - FOUND
- `api/src/GestionAlquileres.API/Middleware/ExceptionMiddleware.cs` - FOUND
- `api/tests/GestionAlquileres.Tests/AuthTests.cs` - FOUND

### Commits Present
- `f5cbe7f` feat(1-03): define IJwtService, JwtSettings, AuthResponseDto, MediatR pipeline behaviors, Application DI
- `e07dba5` feat(1-03): JwtService, auth commands/handlers/validators, ExceptionMiddleware, JWT middleware
- `b5d379b` feat(1-03): BaseController, AuthController, AuthTests integration tests (8/8 passing)

### Test Results
- dotnet test: 12/12 passing (4 pre-existing + 8 new AuthTests)

## Self-Check: PASSED

---
*Phase: 01-scaffolding-multi-tenancy-foundation*
*Completed: 2026-04-13*
