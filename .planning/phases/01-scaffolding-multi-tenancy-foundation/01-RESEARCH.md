# Phase 1: Scaffolding & Multi-tenancy Foundation — Research

**Researched:** 2026-04-12
**Domain:** .NET 8 Clean Architecture + React 19 + PostgreSQL multi-tenancy foundation
**Confidence:** HIGH

---

## Summary

This phase establishes the foundational patterns for the entire application. Every library version,
every architecture decision, and every naming convention established here will propagate into all
8 subsequent phases. The patterns are well-understood and battle-tested — the primary research
risk is version drift between project documentation (AGENTS.md) and actual current releases.

Several significant version discrepancies were found between AGENTS.md and current NuGet/npm
registry: MediatR went from v12 (documented) to v14.1.0 (current); AutoMapper from v13 to
v16.1.1; FluentValidation from v11 to v12.1.1; Serilog.AspNetCore from assumed ~6.x to 10.0.0.
These are not breaking changes for the patterns used, but the correct versions must be targeted
from the start to avoid future upgrade friction.

The machine has .NET 9 SDK (9.0.304) and .NET 10 RC SDK — no .NET 8 SDK installed. A clean
build test confirmed that targeting `net8.0` works correctly with the .NET 9 SDK through
multi-targeting support. A `global.json` pinning the SDK to `9.0.304` is recommended for
reproducibility. PostgreSQL is NOT installed natively; Docker is available (v29.2.1) and must
be used for local PostgreSQL + MinIO development.

**Primary recommendation:** Scaffold the solution with correct current package versions from
day one. Establish the ICurrentTenant/global-filter pattern first in the DbContext — all
future entity registrations follow this template. Use Docker Compose for PostgreSQL locally.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

No CONTEXT.md exists for this phase — this is the initial phase with no prior discussion.
Constraints are sourced from CLAUDE.md (project instructions) and planning documents.

### Locked Decisions (from CLAUDE.md + STATE.md)
- Stack: .NET 8 + React 19 + PostgreSQL + pnpm
- Multi-tenancy: shared DB with OrganizationId as discriminator (NOT separate DB per tenant)
- Storage: MinIO for development, Azure Blob for production
- Clean Architecture: 4 projects — Domain, Application, Infrastructure, API
- OrganizationId extracted from JWT, NEVER from request body
- Global EF Core query filter: `HasQueryFilter(e => e.OrganizationId == _currentTenant.OrganizationId)`
- NEVER `IgnoreQueryFilters()` in production
- NEVER SQL raw (`FromSqlRaw`) except documented edge cases
- Tables: snake_case plural; entities: PascalCase singular
- TypeScript strict — no `any`, no `@ts-ignore`
- NEVER `useEffect` for data fetching — always TanStack Query

### Claude's Discretion
- Exact package versions (within the required major versions)
- Project initialization commands and order
- Docker Compose service configuration details
- Exact JWT token expiry times
- Serilog sink configuration details
- Hangfire dashboard access policy

### Deferred Ideas (OUT OF SCOPE for Phase 1)
- BCRA/INDEC API client implementation (Phase 2)
- Property/Tenant CRUD (Phase 3)
- Contract management (Phase 4)
- Rent calculation logic (Phase 5)
- Document upload/presigned URLs (Phase 6)
- Tenant portal pages (Phase 7)
- Email notifications (Phase 8)
- OAuth/SSO
- Multi-currency support
- AFIP electronic invoicing
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| INFRA-01 | Multi-tenant system with OrganizationId as discriminator in shared DB (PostgreSQL) | EF Core global query filters via HasQueryFilter; ICurrentTenant interface pattern |
| INFRA-02 | JWT includes OrganizationId; global EF Core query filters isolate data per tenant | Microsoft.AspNetCore.Authentication.JwtBearer 8.0.25 + custom claim "org_id" |
| INFRA-03 | Clean Architecture structure: Domain / Application / Infrastructure / API | 4-project .NET solution scaffold; project reference topology |
| INFRA-04 | Hangfire configured for scheduled jobs with PostgreSQL persistence | Hangfire.Core 1.8.23 + Hangfire.PostgreSql 1.21.1 + Hangfire.AspNetCore |
| INFRA-05 | Serilog with structured logging for audit of calculations and access | Serilog.AspNetCore 10.0.0 + Console/File sinks |
| ORG-01 | CRUD for Organizations (registration of real estate admins) | Organization entity + EF config + OrganizationsController |
| ORG-02 | Users with roles: Admin, Staff, Tenant; associated to Organization | User entity with Role enum + BCrypt password hashing |
| ORG-03 | Organization registration creates initial Admin user | POST /api/v1/auth/register-org handler creates both records atomically |
| ORG-04 | JWT authentication (email + password) for Admin/Staff users | LoginCommand + JwtService generating token with org_id + role claims |
| ORG-05 | Separate JWT authentication for Tenant role (portal inquilino) | POST /api/v1/auth/tenant-login endpoint with same flow, different role claim |
</phase_requirements>

---

## Standard Stack

### API (.NET 8) — Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.25 | JWT bearer validation in .NET 8 | Official Microsoft package, matches TFM |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.11 | EF Core provider for PostgreSQL | Official Npgsql, only production-ready PG provider for EF |
| Microsoft.EntityFrameworkCore.Tools | 8.0.25 | dotnet ef CLI migrations | Required for migration generation |
| EFCore.NamingConventions | 8.0.3 | `UseSnakeCaseNamingConvention()` for all tables/columns | Avoids manually specifying `HasColumnName` on every property |
| MediatR | 14.1.0 | CQRS mediator | Standard in .NET Clean Architecture; DI now built-in (no extension package needed) |
| FluentValidation.DependencyInjectionExtensions | 12.1.1 | Validators + DI registration | Auto-registers all `IValidator<T>` implementations |
| AutoMapper | 16.1.1 | Entity → DTO mapping | DI now built-in since v13; `AddAutoMapper(typeof(X).Assembly)` |
| BCrypt.Net-Next | 4.1.0 | Password hashing | Industry standard bcrypt; work factor configurable |
| Hangfire.Core | 1.8.23 | Job scheduling infrastructure | Standard Hangfire package |
| Hangfire.AspNetCore | 1.8.23 | Hangfire ASP.NET Core integration | Adds `AddHangfire`, `UseHangfireServer`, dashboard |
| Hangfire.PostgreSql | 1.21.1 | PostgreSQL storage for Hangfire | Community provider, widely used |
| Serilog.AspNetCore | 10.0.0 | Structured logging integration | Replaces default Microsoft logger |
| Serilog.Sinks.Console | 6.1.1 | Console output for Serilog | Development logging |
| Serilog.Sinks.File | 7.0.0 | File output for Serilog | Audit trail persistence |
| Serilog.Enrichers.Environment | 3.0.1 | Machine name / env enrichment | Structured log metadata |
| Swashbuckle.AspNetCore | 10.1.7 | Swagger/OpenAPI UI | API exploration during development |

[VERIFIED: npm registry / NuGet registry - all versions confirmed via direct API queries 2026-04-12]

### API (.NET 8) — Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.IdentityModel.Tokens.Jwt | 8.17.0 | JWT token generation (signing) | In JwtService to create tokens |
| Microsoft.Extensions.Options | (transitively included) | Options pattern for JwtSettings | Config binding |

[VERIFIED: NuGet registry]

### Frontend (React 19) — Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| react + react-dom | 19.2.5 | UI framework | Locked decision |
| typescript | 6.0.2 | Type safety (strict mode) | Locked decision |
| vite | 8.0.8 | Build tool / dev server | Locked decision |
| @vitejs/plugin-react | 6.0.1 | React Fast Refresh in Vite | Official Vite + React integration |
| @tanstack/react-query | 5.99.0 | Server state management | Locked decision; replaces useEffect for fetching |
| react-router | 7.14.0 | Client-side routing | Locked decision |
| zustand | 5.0.12 | Client UI state | Locked decision; auth token storage |
| axios | 1.15.0 | HTTP client with interceptors | Locked decision; JWT auto-attach |
| tailwindcss | 4.2.2 | Utility-first CSS | Locked decision |
| @tailwindcss/vite | 4.2.2 | Tailwind v4 Vite plugin | Required for Tailwind v4 (replaces PostCSS config) |
| shadcn (CLI) | 4.2.0 | Component scaffolder | Locked decision; copies components into codebase |
| react-hook-form | 7.72.1 | Form state management | Documented in AGENTS.md |
| zod | 4.3.6 | Schema validation | Documented in AGENTS.md |
| @hookform/resolvers | 5.2.2 | Zod integration with RHF | Required bridge between react-hook-form and zod |
| lucide-react | 1.8.0 | Icon library | shadcn/ui dependency |

[VERIFIED: npm registry - all versions confirmed 2026-04-12]

### Frontend (React 19) — Dev/Testing

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| vitest | 4.1.4 | Test runner | `pnpm test` |
| @testing-library/react | 16.3.2 | React component testing | Unit tests for components |
| @testing-library/user-event | 14.6.1 | User interaction simulation | Form and click testing |
| jsdom | 29.0.2 | DOM environment for Vitest | Vitest environment config |
| eslint | 10.2.0 | Linting (zero warnings required) | `pnpm lint` |

[VERIFIED: npm registry]

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| EFCore.NamingConventions | Manual `HasColumnName` per property | NamingConventions is DRY; manual is explicit but verbose |
| BCrypt.Net-Next | ASP.NET Core Identity (full stack) | Identity adds User/Role tables and middleware — overkill for this custom multi-tenant model |
| AutoMapper | Mapster | Both work; AutoMapper is documented in AGENTS.md — do not deviate |
| Hangfire | Quartz.NET | Hangfire has better dashboard UX and PostgreSQL storage; Quartz is lower level |

### Installation Commands

```bash
# .NET 8 solution scaffold
dotnet new sln -n GestionAlquileres -o api
cd api
dotnet new classlib -n GestionAlquileres.Domain -o src/GestionAlquileres.Domain --framework net8.0
dotnet new classlib -n GestionAlquileres.Application -o src/GestionAlquileres.Application --framework net8.0
dotnet new classlib -n GestionAlquileres.Infrastructure -o src/GestionAlquileres.Infrastructure --framework net8.0
dotnet new webapi -n GestionAlquileres.API -o src/GestionAlquileres.API --framework net8.0
dotnet sln add src/GestionAlquileres.Domain/GestionAlquileres.Domain.csproj
dotnet sln add src/GestionAlquileres.Application/GestionAlquileres.Application.csproj
dotnet sln add src/GestionAlquileres.Infrastructure/GestionAlquileres.Infrastructure.csproj
dotnet sln add src/GestionAlquileres.API/GestionAlquileres.API.csproj
```

```bash
# NuGet packages — Infrastructure project
dotnet add src/GestionAlquileres.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.11
dotnet add src/GestionAlquileres.Infrastructure package Microsoft.EntityFrameworkCore.Tools --version 8.0.25
dotnet add src/GestionAlquileres.Infrastructure package EFCore.NamingConventions --version 8.0.3
dotnet add src/GestionAlquileres.Infrastructure package Hangfire.Core --version 1.8.23
dotnet add src/GestionAlquileres.Infrastructure package Hangfire.AspNetCore --version 1.8.23
dotnet add src/GestionAlquileres.Infrastructure package Hangfire.PostgreSql --version 1.21.1
dotnet add src/GestionAlquileres.Infrastructure package AutoMapper --version 16.1.1
dotnet add src/GestionAlquileres.Infrastructure package BCrypt.Net-Next --version 4.1.0

# NuGet packages — Application project
dotnet add src/GestionAlquileres.Application package MediatR --version 14.1.0
dotnet add src/GestionAlquileres.Application package FluentValidation.DependencyInjectionExtensions --version 12.1.1
dotnet add src/GestionAlquileres.Application package AutoMapper --version 16.1.1

# NuGet packages — API project
dotnet add src/GestionAlquileres.API package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.25
dotnet add src/GestionAlquileres.API package System.IdentityModel.Tokens.Jwt --version 8.17.0
dotnet add src/GestionAlquileres.API package Serilog.AspNetCore --version 10.0.0
dotnet add src/GestionAlquileres.API package Serilog.Sinks.Console --version 6.1.1
dotnet add src/GestionAlquileres.API package Serilog.Sinks.File --version 7.0.0
dotnet add src/GestionAlquileres.API package Serilog.Enrichers.Environment --version 3.0.1
dotnet add src/GestionAlquileres.API package Swashbuckle.AspNetCore --version 10.1.7
```

```bash
# Frontend scaffold
cd web
pnpm create vite@latest . --template react-ts
pnpm install
pnpm add @tanstack/react-query react-router zustand axios react-hook-form zod @hookform/resolvers
pnpm add tailwindcss @tailwindcss/vite lucide-react
pnpm add -D vitest @testing-library/react @testing-library/user-event jsdom @vitejs/plugin-react

# shadcn/ui initialization (after Tailwind setup)
pnpm dlx shadcn@latest init
```

[VERIFIED: npm registry 2026-04-12]

---

## Architecture Patterns

### Recommended Project Structure

```
api/
  global.json                   # Pin SDK version to 9.0.304
  GestionAlquileres.sln
  src/
    GestionAlquileres.Domain/
      Entities/
        Organization.cs
        User.cs
        ITenantEntity.cs         # Interface marker for multi-tenant entities
      Enums/
        UserRole.cs
        AdjustmentType.cs        # Defined now for future phases
        ContractStatus.cs
      Interfaces/
        Repositories/
          IOrganizationRepository.cs
          IUserRepository.cs
        Services/
          IJwtService.cs
          ICurrentTenant.cs      # CRITICAL: interface used in DbContext
    GestionAlquileres.Application/
      Features/
        Auth/
          Commands/
            RegisterOrgCommand.cs
            LoginCommand.cs
            TenantLoginCommand.cs
          Handlers/
            RegisterOrgCommandHandler.cs
            LoginCommandHandler.cs
          Validators/
            RegisterOrgCommandValidator.cs
            LoginCommandValidator.cs
          DTOs/
            AuthResponseDto.cs
            LoginRequest.cs
      Common/
        Behaviors/
          ValidationBehavior.cs   # FluentValidation pipeline behavior
          LoggingBehavior.cs
        Interfaces/               # Re-export ICurrentTenant from Domain
    GestionAlquileres.Infrastructure/
      Persistence/
        AppDbContext.cs
        Repositories/
          OrganizationRepository.cs
          UserRepository.cs
        Configurations/
          OrganizationConfiguration.cs
          UserConfiguration.cs
        Migrations/
      Services/
        CurrentTenantService.cs   # Reads OrganizationId from IHttpContextAccessor
        JwtService.cs
      Jobs/                        # Empty in Phase 1, Hangfire jobs added in Phase 5
      DependencyInjection.cs       # Infrastructure service registrations
    GestionAlquileres.API/
      Controllers/
        BaseController.cs
        AuthController.cs
      Middleware/
        ExceptionMiddleware.cs
      Extensions/
        ServiceExtensions.cs      # AddApplication, AddInfrastructure helpers
      Program.cs

web/
  src/
    features/
      auth/
        components/
          LoginForm.tsx
          RegisterOrgForm.tsx
        hooks/
          useLogin.ts
          useRegisterOrg.ts
        services/
          authService.ts
        types/
          auth.types.ts
    portal-admin/
      layouts/
        AdminLayout.tsx
      pages/
        DashboardPage.tsx
      routes.tsx
    portal-inquilino/
      layouts/
        InquilinoLayout.tsx
      pages/
        LoginPage.tsx            # Separate login for tenants
      routes.tsx
    shared/
      components/               # shadcn/ui components copied here
      hooks/
        useAuth.ts
      lib/
        api.ts                  # Axios instance with interceptors
        queryClient.ts
      stores/
        authStore.ts            # Zustand — token + user info
      types/
        common.types.ts
      lib/
        formatters.ts           # formatARS, formatDate, formatPct
    App.tsx
    main.tsx
  vite.config.ts
  tsconfig.json
  components.json               # shadcn/ui config
  docker-compose.yml
```

### Pattern 1: ICurrentTenant — Multi-tenancy Context Propagation

**What:** An interface injected into AppDbContext that exposes the current request's OrganizationId. Populated by middleware reading the JWT claim.

**When to use:** Always. This is the foundational pattern. Every entity with `OrganizationId` must use it in `HasQueryFilter`.

```csharp
// Domain/Interfaces/Services/ICurrentTenant.cs
// Source: api/AGENTS.md (project documentation)
public interface ICurrentTenant
{
    Guid OrganizationId { get; }
}

// Infrastructure/Services/CurrentTenantService.cs
// [VERIFIED: pattern aligned with Microsoft EF Core multi-tenancy docs]
public class CurrentTenantService : ICurrentTenant
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid OrganizationId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User
                .FindFirstValue("org_id");
            return claim is not null
                ? Guid.Parse(claim)
                : Guid.Empty; // Unauthenticated requests — global filter returns no rows
        }
    }
}
```

**Critical registration order:** `IHttpContextAccessor` must be registered before `ICurrentTenant`.
```csharp
services.AddHttpContextAccessor();
services.AddScoped<ICurrentTenant, CurrentTenantService>();
```

### Pattern 2: AppDbContext Global Query Filter

**What:** EF Core's `HasQueryFilter` is called once in `OnModelCreating`. It captures a closure over `_currentTenant`. EF Core applies it as a WHERE clause to every query on that entity type.

**CRITICAL caveat:** `OnModelCreating` runs once per application lifetime, but `_currentTenant` is resolved at query time via DI. This works because EF Core evaluates the filter expression at query execution time — not at model-building time.

```csharp
// Infrastructure/Persistence/AppDbContext.cs
// Source: api/AGENTS.md (project documentation)
public class AppDbContext : DbContext
{
    private readonly ICurrentTenant _currentTenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenant currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Multi-tenant global filters
        // IndexValue has NO filter — BCRA indices are global data
        modelBuilder.Entity<Organization>().HasQueryFilter(
            e => e.OrganizationId == _currentTenant.OrganizationId);
        modelBuilder.Entity<User>().HasQueryFilter(
            e => e.OrganizationId == _currentTenant.OrganizationId);
    }
}
```

**Registration:**
```csharp
services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention();  // EFCore.NamingConventions
});
```

### Pattern 3: MediatR v14 — CQRS Pipeline

**What:** MediatR 14 includes DI support natively. No separate `MediatR.Extensions.Microsoft.DependencyInjection` package needed. Pipeline behaviors use `AddOpenBehavior`.

```csharp
// Program.cs / Application DI registration
// [VERIFIED: MediatR 12.0 release notes — DI extensions merged into core]
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(RegisterOrgCommand).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});
```

**ValidationBehavior pattern:**
```csharp
// Application/Common/Behaviors/ValidationBehavior.cs
// [VERIFIED: FluentValidation docs + MediatR open behavior pattern]
public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

**FluentValidation auto-registration (v12):**
```csharp
// Registers all AbstractValidator<T> implementations from assembly
services.AddValidatorsFromAssembly(typeof(RegisterOrgCommand).Assembly);
```

### Pattern 4: JWT Authentication with OrganizationId Claim

**What:** Bearer token auth where the token payload includes `org_id` (OrganizationId) and `role`. The claim name `org_id` is extracted by `CurrentTenantService`.

```csharp
// Infrastructure/Services/JwtService.cs
// [ASSUMED — standard JWT creation pattern; claim names are project decisions]
public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;

    public string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("org_id", user.OrganizationId.ToString()),  // CRITICAL: extracted by ICurrentTenant
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_settings.ExpiryHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// API Extensions — JWT middleware registration
// [VERIFIED: Microsoft.AspNetCore.Authentication.JwtBearer docs]
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
    });
```

### Pattern 5: BaseController — OrganizationId from JWT

**What:** All controllers extend `BaseController`. It exposes `OrganizationId` property parsed from the `org_id` JWT claim. No controller or command ever reads OrganizationId from the request body.

```csharp
// API/Controllers/BaseController.cs
// Source: api/AGENTS.md
public abstract class BaseController : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    // Parsed from JWT — never from request body
    protected Guid OrganizationId =>
        Guid.Parse(User.FindFirstValue("org_id")!);
}
```

### Pattern 6: AutoMapper v16 — Profile Registration

```csharp
// Application/Common/Mappings/OrganizationProfile.cs
// [VERIFIED: AutoMapper 13+ DI docs — AddAutoMapper built into core package]
public class OrganizationProfile : Profile
{
    public OrganizationProfile()
    {
        CreateMap<Organization, OrganizationDto>();
        CreateMap<User, UserDto>();
    }
}

// Registration (no separate DI extension package needed since AutoMapper 13)
services.AddAutoMapper(typeof(OrganizationProfile).Assembly);
```

### Pattern 7: Hangfire PostgreSQL Setup

```csharp
// [VERIFIED: Hangfire.PostgreSql 1.21.1 documentation + NuGet]
services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
    {
        options.UseNpgsqlConnection(connectionString);
    }));

services.AddHangfireServer();

// Dashboard (development only or restrict in production)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    IsReadOnlyFunc = _ => !app.Environment.IsDevelopment()
});
```

### Pattern 8: Serilog v10 Setup

```csharp
// Program.cs
// [VERIFIED: Serilog.AspNetCore 10.0.0 — UseSerilog API unchanged]
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.File("logs/app-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));
```

### Pattern 9: Tailwind v4 + Vite Configuration

**IMPORTANT:** Tailwind v4 removes the `tailwind.config.js` and PostCSS approach. Use `@tailwindcss/vite` plugin instead.

```typescript
// vite.config.ts
// [VERIFIED: @tailwindcss/vite 4.2.2 — Tailwind v4 Vite integration]
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
})
```

```css
/* src/index.css — Tailwind v4 import syntax */
@import "tailwindcss";
```

**No `tailwind.config.js` needed for v4.** Content detection is automatic.

### Pattern 10: Axios Interceptors — JWT Auto-attach

```typescript
// shared/lib/api.ts
// Source: web/AGENTS.md
import axios from 'axios'
import { useAuthStore } from '../stores/authStore'

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
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
  (error) => {
    if (error.response?.status === 401) {
      useAuthStore.getState().logout()
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)
```

### Pattern 11: EF Core Entity Configuration (snake_case tables)

```csharp
// Infrastructure/Persistence/Configurations/OrganizationConfiguration.cs
// [VERIFIED: EFCore.NamingConventions docs — UseSnakeCaseNamingConvention handles naming automatically]
public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        // Table name snake_case handled by UseSnakeCaseNamingConvention
        // BUT explicit table name needed because plural convention requires manual setting
        builder.ToTable("organizations");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(o => o.Slug).IsUnique();

        builder.Property(o => o.Plan)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("free");

        builder.Property(o => o.IsActive)
            .HasDefaultValue(true);

        builder.Property(o => o.CreatedAt)
            .HasDefaultValueSql("now()");
    }
}
```

**Note:** `UseSnakeCaseNamingConvention()` handles column names automatically (e.g., `OrganizationId` → `organization_id`). Table names still need explicit `ToTable("organizations")` to enforce plural form.

### Anti-Patterns to Avoid

- **Direct DbContext access from controllers:** Always go through Repository interfaces. Controllers call MediatR → Handlers call Repositories.
- **OrganizationId from request body:** NEVER. Controllers derive it from `User.FindFirstValue("org_id")` via `BaseController.OrganizationId`.
- **`IgnoreQueryFilters()` in production:** Breaks tenant isolation. Only allowed in migrations, admin tooling, and index sync (IndexValue has no filter by design).
- **`AddMediatR` + `MediatR.Extensions.Microsoft.DependencyInjection` together:** Causes duplicate registration. MediatR 12+ includes DI natively.
- **`AutoMapper.Extensions.Microsoft.DependencyInjection` with AutoMapper 13+:** Same issue — DI is in the core package.
- **Tailwind v4 with `tailwind.config.js`:** v4 uses the Vite plugin. Using `postcss.config.js` + `tailwind.config.js` is the v3 approach.
- **Multiple `HasQueryFilter` on same entity:** EF Core 9 and below supports only one `HasQueryFilter` per entity. If you need to add a second filter, combine them with `&&`.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Password hashing | Custom crypto | BCrypt.Net-Next | bcrypt handles salting, work factors, and rainbow table resistance automatically |
| JWT validation | Custom token parsing | Microsoft.AspNetCore.Authentication.JwtBearer | Clock skew, revocation hooks, claims extraction — all handled |
| DB migrations | Manual SQL scripts | EF Core migrations | Schema drift, ordering, rollback tracking — migrations are reproducible |
| snake_case column names | `HasColumnName` on every property | EFCore.NamingConventions | 50+ columns — manually specifying each is error-prone and verbose |
| Request validation | Manual if/else in handlers | FluentValidation + ValidationBehavior | Declarative, composable, automatic 400 responses |
| Object mapping | Manual `new Dto { X = entity.X }` | AutoMapper profiles | Null safety, nested mapping, testable profiles |
| Background jobs | Timer + Task.Run | Hangfire | Persistence, retries, dashboard, PostgreSQL storage |
| Query filtering for multi-tenancy | WHERE clause in every repository method | EF Core global query filters | Missing one query = data leak across tenants |

**Key insight:** The multi-tenancy filter is the single most dangerous area. One missed `WHERE organization_id = ?` clause exposes all tenants' data. EF Core global filters make this impossible to miss by construction.

---

## Common Pitfalls

### Pitfall 1: HasQueryFilter Breaks for Unauthenticated Requests

**What goes wrong:** When `ICurrentTenant.OrganizationId` returns `Guid.Empty` (unauthenticated request reaches a protected endpoint somehow), the filter `WHERE organization_id = '00000000-...'` returns no rows instead of throwing. Silent data suppression.

**Why it happens:** The filter is always active. Guid.Empty matches nothing.

**How to avoid:** All tenant-protected endpoints MUST have `[Authorize]` attribute. Add integration test: unauthenticated request to protected endpoint returns 401, not empty list.

**Warning signs:** Protected endpoint returns empty list when it should return 401.

### Pitfall 2: EF Core DbContext Scoping with Hangfire

**What goes wrong:** Hangfire jobs run in their own scope. If the job resolves `AppDbContext` directly from a singleton or root scope, the `ICurrentTenant` will have no HTTP context — `OrganizationId` returns `Guid.Empty`.

**Why it happens:** Hangfire jobs are not HTTP requests; `IHttpContextAccessor.HttpContext` is null.

**How to avoid:** In Phase 5 when Hangfire jobs are implemented — jobs must either: (a) accept an explicit `organizationId` parameter and scope the DbContext with a custom `ICurrentTenant` implementation, or (b) use `IgnoreQueryFilters()` explicitly when processing all tenants in a batch. Phase 1 only configures Hangfire infrastructure; this pitfall is documented now for future phases.

### Pitfall 3: MediatR ValidationBehavior Swallows Validation Errors

**What goes wrong:** If `ExceptionMiddleware` doesn't catch `ValidationException`, it returns 500 instead of 400.

**Why it happens:** FluentValidation's `ValidationException` is not a built-in ASP.NET Core exception type.

**How to avoid:** `ExceptionMiddleware` must explicitly catch `FluentValidation.ValidationException` and return `400 BadRequest` with the list of errors formatted as `{ field, message }` objects.

### Pitfall 4: Tailwind v4 Config Confusion

**What goes wrong:** AI-generated setup instructions may use v3 config (`tailwind.config.js`, `@tailwindcss/postcss`). With v4, this either fails silently or outputs unstyled HTML.

**Why it happens:** Tailwind v4 (released 2025) is a major paradigm shift. Training data has more v3 examples.

**How to avoid:** Use `@tailwindcss/vite` plugin. Import with `@import "tailwindcss"` in CSS. No `tailwind.config.js`. No `postcss.config.js`. [VERIFIED: @tailwindcss/vite 4.2.2 on npm registry]

### Pitfall 5: Project Reference Topology Violation

**What goes wrong:** Adding `EF Core` or `HttpClient` reference to `Domain` project. This violates Clean Architecture and makes Domain non-portable.

**Why it happens:** Convenience — placing EF annotations directly on entities.

**How to avoid:** Domain project references NOTHING except core .NET libraries. All EF configurations live in `Infrastructure/Persistence/Configurations/` as `IEntityTypeConfiguration<T>` classes. Use `UseSnakeCaseNamingConvention()` + `ApplyConfigurationsFromAssembly()` to keep configurations separate.

### Pitfall 6: .NET 9 SDK with net8.0 Target — global.json

**What goes wrong:** Different developers have different SDK versions. .NET 10 RC (also installed) may be used accidentally, which has breaking changes.

**Why it happens:** The machine has both .NET 9 (9.0.304) and .NET 10 RC SDK installed.

**How to avoid:** Add `global.json` to pin the SDK:
```json
{
  "sdk": {
    "version": "9.0.304",
    "rollForward": "latestPatch"
  }
}
```

### Pitfall 7: shadcn/ui Components Path Alias

**What goes wrong:** `pnpm dlx shadcn@latest init` fails if `@` path alias is not configured in both `vite.config.ts` AND `tsconfig.json`/`tsconfig.app.json` before running shadcn init.

**Why it happens:** shadcn reads `components.json` which references `@/components/ui` — both TypeScript compiler and Vite bundler need to resolve the `@` alias.

**How to avoid:** Configure alias in `vite.config.ts` and `tsconfig.app.json` first, then run `pnpm dlx shadcn@latest init`.

---

## Code Examples

### Register-Org Flow (Command + Handler)

```csharp
// Application/Features/Auth/Commands/RegisterOrgCommand.cs
// [ASSUMED — standard CQRS registration pattern]
public record RegisterOrgCommand(
    string OrganizationName,
    string Slug,
    string AdminEmail,
    string AdminPassword,
    string AdminFirstName,
    string AdminLastName
) : IRequest<AuthResponseDto>;

public class RegisterOrgCommandHandler : IRequestHandler<RegisterOrgCommand, AuthResponseDto>
{
    private readonly IOrganizationRepository _orgRepo;
    private readonly IUserRepository _userRepo;
    private readonly IJwtService _jwtService;
    private readonly AppDbContext _dbContext;  // for transaction

    public async Task<AuthResponseDto> Handle(RegisterOrgCommand request, CancellationToken ct)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            var org = new Organization
            {
                Name = request.OrganizationName,
                Slug = request.Slug.ToLowerInvariant(),
                Plan = "free",
                IsActive = true,
            };
            await _orgRepo.AddAsync(org, ct);

            var user = new User
            {
                OrganizationId = org.Id,       // Set explicitly — no ICurrentTenant needed for registration
                Email = request.AdminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword),
                FirstName = request.AdminFirstName,
                LastName = request.AdminLastName,
                Role = UserRole.Admin,
                IsActive = true,
            };
            await _userRepo.AddAsync(user, ct);
            await tx.CommitAsync(ct);

            var token = _jwtService.GenerateToken(user);
            return new AuthResponseDto(token, user.Email, user.Role.ToString());
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
```

**Note on ICurrentTenant in registration:** The `RegisterOrg` flow creates a new org and its first user — the OrganizationId is assigned explicitly, not from `ICurrentTenant`. This is the only legitimate case where OrganizationId is not extracted from JWT (because no JWT exists yet).

### Zustand Auth Store

```typescript
// shared/stores/authStore.ts
// Source: web/AGENTS.md pattern
import { create } from 'zustand'
import { persist } from 'zustand/middleware'

interface AuthState {
  token: string | null
  user: { email: string; role: string } | null
  login: (token: string, user: AuthState['user']) => void
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
    { name: 'auth-storage' }  // localStorage key
  )
)
```

### React Router v7 — Dual Portal Routing

```typescript
// App.tsx
// [VERIFIED: react-router 7.14.0 — createBrowserRouter API]
import { createBrowserRouter, RouterProvider } from 'react-router'
import { adminRoutes } from './portal-admin/routes'
import { inquilinoRoutes } from './portal-inquilino/routes'

const router = createBrowserRouter([
  {
    path: '/admin',
    children: adminRoutes,
  },
  {
    path: '/inquilino',
    children: inquilinoRoutes,
  },
  {
    path: '/',
    element: <Navigate to="/admin/login" replace />,
  },
])

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  )
}
```

### Docker Compose for Local Development

```yaml
# docker-compose.yml (project root)
# [ASSUMED — standard PostgreSQL + MinIO Docker Compose configuration]
version: '3.9'
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: gestion_alquileres
      POSTGRES_USER: appuser
      POSTGRES_PASSWORD: devpassword
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  minio:
    image: minio/minio:latest
    command: server /data --console-address ":9001"
    environment:
      MINIO_ROOT_USER: minioadmin
      MINIO_ROOT_PASSWORD: minioadmin
    ports:
      - "9000:9000"   # API
      - "9001:9001"   # Console
    volumes:
      - minio_data:/data

volumes:
  postgres_data:
  minio_data:
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| MediatR.Extensions.Microsoft.DependencyInjection separate package | DI built into MediatR 12+ core | MediatR 12.0 (2023) | Remove extension package from dependency list |
| AutoMapper.Extensions.Microsoft.DependencyInjection separate package | DI built into AutoMapper 13+ core | AutoMapper 13.0 (2023) | Same — remove extension package |
| Tailwind v3 `tailwind.config.js` + PostCSS | Tailwind v4 `@tailwindcss/vite` plugin | Tailwind v4.0 (2025) | No config file; different import syntax |
| `app.UseHangfireServer()` | `app.UseHangfireServer()` (unchanged) | — | No change |
| FluentValidation v11 `IValidator<T>` | FluentValidation v12 `IValidator<T>` | FluentValidation 12.0 (2024) | Mostly additive — same `AbstractValidator<T>` base |

**Deprecated/outdated (per AGENTS.md):**
- AGENTS.md references MediatR 12 → current is 14.1.0. Use 14.1.0.
- AGENTS.md references FluentValidation 11 → current is 12.1.1. Use 12.1.1.
- AGENTS.md references AutoMapper 13 → current is 16.1.1. Use 16.1.1.
- AGENTS.md references Vite 6 → current is 8.0.8. Use current.
- AGENTS.md references TailwindCSS 4 (correct) and Vite 6 → Vite is now 8.0.8.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | JWT token expiry of N hours is acceptable (value not specified in requirements) | Pattern 4: JWT | Minor — easily changed via config; no schema impact |
| A2 | BCrypt work factor of 11 (default) is appropriate for this use case | Standard Stack | Security tradeoff — too low = weak; too high = slow login. 11 is widely accepted default |
| A3 | Docker Compose `postgres:16-alpine` image version is appropriate | Docker Compose example | PostgreSQL 16 is current stable; image availability on dev machines |
| A4 | `global.json` pinning to SDK 9.0.304 is correct for this dev environment | Pitfall 6 | If other developers have only .NET 8 SDK, they need to update |
| A5 | `org_id` is the correct JWT claim name for OrganizationId | Pattern 4, BaseController | Claim name must match exactly between `JwtService` and `CurrentTenantService`; inconsistency causes Guid.Empty tenant = no data returned |
| A6 | Hangfire dashboard at `/hangfire` requires no auth in development | Pattern 7 | Security concern in staging — restrict in non-dev environments |

---

## Open Questions

1. **Should Organization entity also have a HasQueryFilter?**
   - What we know: The `organizations` table contains one row per tenant. The `org_id` claim identifies the org.
   - What's unclear: Filtering `Organizations` by `OrganizationId` where `OrganizationId` IS the PK seems circular. The ERD shows no `OrganizationId` FK on `organizations` itself.
   - Recommendation: Do NOT apply global filter to `Organization` entity. Query it by PK (`Id`) directly. The global filter applies to all OTHER entities that have `organization_id` FK.

2. **TailwindCSS v4 + shadcn/ui compatibility**
   - What we know: shadcn/ui officially supports Vite + Tailwind v4 (ui.shadcn.com/docs/installation/vite)
   - What's unclear: Some older shadcn/ui component templates may still reference v3 patterns
   - Recommendation: Use `pnpm dlx shadcn@latest init` which generates v4-compatible output. Check `components.json` post-init.

3. **Plural table names with EFCore.NamingConventions**
   - What we know: `UseSnakeCaseNamingConvention()` handles column names automatically. Table names need explicit `ToTable("organizations")`.
   - What's unclear: Does the convention also pluralize table names automatically?
   - Recommendation: Always specify `ToTable()` explicitly in configurations — do not rely on convention for table name pluralization.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | API build, run, test | ✓ | 9.0.304 (targets net8.0) | — |
| Node.js | Frontend build, pnpm | ✓ | 22.14.0 | — |
| pnpm | Frontend package management | ✓ | 10.33.0 | — |
| Docker | PostgreSQL + MinIO local | ✓ | 29.2.1 | Install PostgreSQL natively |
| PostgreSQL (native) | DB direct connect | ✗ | — | Docker Compose (recommended) |
| PostgreSQL (Docker) | DB via Docker | Not running yet | — | Start via `docker compose up -d` |
| MinIO (Docker) | Object storage local | Not running yet | — | Start via `docker compose up -d` |
| psql CLI | Migration verification | ✗ | — | Use DBeaver or TablePlus for visual inspection |

**Missing dependencies with no fallback:**
- None — Docker covers all infrastructure needs.

**Missing dependencies with fallback:**
- PostgreSQL (native): Docker Compose is the primary approach. Plan must include `docker compose up -d` as Wave 0 step.
- psql CLI: Not needed for the plan; EF Core migrations run via `dotnet ef database update`.

---

## Validation Architecture

Config `testRequirement: "required"` — Nyquist validation is enabled (no `workflow.nyquist_validation` key found, treating as enabled).

### Test Framework — API

| Property | Value |
|----------|-------|
| Framework | xUnit (dotnet new webapi includes it; standard for .NET) |
| Config file | none — discovered via project reference |
| Quick run command | `cd api && dotnet test --filter "Category=Unit" --no-build` |
| Full suite command | `cd api && dotnet test` |

### Test Framework — Web

| Property | Value |
|----------|-------|
| Framework | Vitest 4.1.4 |
| Config file | `web/vite.config.ts` (vitest config in same file) |
| Quick run command | `cd web && pnpm test --run` |
| Full suite command | `cd web && pnpm test` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| INFRA-01 | OrganizationId filter isolates data between tenants | Integration | `dotnet test --filter "TenantIsolation"` | ❌ Wave 0 |
| INFRA-02 | JWT includes org_id claim; DbContext filter applied | Integration | `dotnet test --filter "JwtClaims"` | ❌ Wave 0 |
| INFRA-03 | Solution builds without errors | Build smoke | `dotnet build` | ❌ Wave 0 |
| INFRA-04 | Hangfire is configured and dashboard is reachable | Smoke | `dotnet run` + HTTP probe | ❌ Wave 0 |
| INFRA-05 | Serilog outputs structured JSON log entries | Unit | `dotnet test --filter "Logging"` | ❌ Wave 0 |
| ORG-01 | POST /auth/register-org creates org + admin user | Integration | `dotnet test --filter "RegisterOrg"` | ❌ Wave 0 |
| ORG-02 | Users have correct roles; password is hashed (BCrypt) | Unit | `dotnet test --filter "UserRoles"` | ❌ Wave 0 |
| ORG-03 | Registration is atomic — both org and user created | Integration | `dotnet test --filter "RegisterOrgAtomic"` | ❌ Wave 0 |
| ORG-04 | POST /auth/login returns JWT with org_id claim | Integration | `dotnet test --filter "LoginJwt"` | ❌ Wave 0 |
| ORG-05 | POST /auth/tenant-login returns JWT with Tenant role | Integration | `dotnet test --filter "TenantLogin"` | ❌ Wave 0 |
| WEB | pnpm build exits 0 with no TypeScript errors | Build smoke | `pnpm build` | ❌ Wave 0 |
| WEB | Login form validation (required fields, email format) | Unit | `pnpm test --run` | ❌ Wave 0 |

### Sampling Rate

- **Per task commit:** `dotnet build && cd ../web && pnpm build` (both sides compile)
- **Per wave merge:** `cd api && dotnet test && cd ../web && pnpm test --run`
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps

- [ ] `api/tests/GestionAlquileres.Tests/` — create test project: `dotnet new xunit -n GestionAlquileres.Tests -o api/tests/GestionAlquileres.Tests --framework net8.0`
- [ ] `api/tests/GestionAlquileres.Tests/TenantIsolationTests.cs` — multi-tenant filter integration tests
- [ ] `api/tests/GestionAlquileres.Tests/AuthTests.cs` — register, login, JWT claims tests
- [ ] `web/src/features/auth/__tests__/LoginForm.test.tsx` — form validation tests

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | BCrypt.Net-Next for hashing; JWT bearer validation |
| V3 Session Management | yes | JWT with expiry; Zustand persist for client-side storage (localStorage) |
| V4 Access Control | yes | `[Authorize]` on all controllers; role-based claims in JWT |
| V5 Input Validation | yes | FluentValidation in MediatR pipeline; Zod on frontend |
| V6 Cryptography | yes | HMAC-SHA256 JWT signing; BCrypt password hashing — never hand-roll |

### Known Threat Patterns for This Stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Tenant data leakage | Information Disclosure | EF Core global query filter; never `IgnoreQueryFilters()` |
| Tenant ID spoofing via request body | Elevation of Privilege | Always extract OrganizationId from JWT claim — never from `[FromBody]` |
| JWT secret exposure | Information Disclosure | Store in `appsettings.Development.json` (gitignored) or environment variables; never hardcode |
| Mass assignment / binding | Tampering | Use explicit Command records (not binding directly to entities) |
| SQL injection | Tampering | EF Core LINQ only; `FromSqlRaw` forbidden |
| Weak password hashing | Information Disclosure | BCrypt with work factor ≥ 11; never MD5/SHA1 |
| Hangfire dashboard exposure | Information Disclosure | Restrict access in non-dev environments via `IsReadOnlyFunc` or auth policy |

---

## Project Constraints (from CLAUDE.md)

| Directive | Category | Impact on Phase 1 |
|-----------|----------|-------------------|
| Multi-tenancy always — every EF Core query via global filter | Critical rule | DbContext must configure `HasQueryFilter` for all entities before any other work |
| OrganizationId from JWT — never from request body | Critical rule | BaseController exposes `protected Guid OrganizationId` from claims; commands receive only business fields |
| Indices persisted before calculation | Critical rule | Not applicable in Phase 1, but the `IndexValue` entity must be designed without global filter |
| NEVER `IgnoreQueryFilters()` in production | Critical rule | Test must verify filter is active |
| NEVER `FromSqlRaw` | Critical rule | Repository implementations use only LINQ |
| TypeScript strict — no `any`, no `@ts-ignore` | Critical rule | `tsconfig.json` must have `"strict": true` |
| NEVER `useEffect` for data fetching | Critical rule | All API calls via TanStack Query hooks |
| Tables: snake_case plural | Naming convention | `ToTable("organizations")` in entity configs + `UseSnakeCaseNamingConvention()` |
| Entities: singular PascalCase | Naming convention | `Organization`, `User` — not `Organizations`, `Users` |
| Commands: `{Action}{Resource}Command` | Naming convention | `RegisterOrgCommand`, `LoginCommand`, `TenantLoginCommand` |
| Handlers: `{Command}Handler` | Naming convention | `RegisterOrgCommandHandler` |
| API layer: no business logic | Architecture rule | Controllers: validate JWT, extract OrganizationId, call MediatR, return result |
| Domain layer: no EF/HTTP references | Architecture rule | Domain project must NOT reference `Microsoft.EntityFrameworkCore` |
| `pnpm lint` zero warnings | Frontend rule | ESLint configured with zero-warning policy from start |

---

## Sources

### Primary (HIGH confidence)
- NuGet registry (api.nuget.org) — all .NET package versions verified 2026-04-12
- npm registry — all Node.js package versions verified 2026-04-12
- `api/AGENTS.md` — project-specific patterns and code examples
- `web/AGENTS.md` — project-specific React patterns and code examples
- `CLAUDE.md` — authoritative project constraints and naming conventions
- `.planning/ERD-AND-API.md` — entity schema and API endpoint specification

### Secondary (MEDIUM confidence)
- [MediatR 12.0 Released — jimmybogard.com](https://www.jimmybogard.com/mediatr-12-0-released/) — confirmed DI extension merged into core
- [AutoMapper 13.0 Released — jimmybogard.com](https://www.jimmybogard.com/automapper-13-0-released/) — confirmed DI extension merged into core
- [EF Core Multi-tenancy — Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/miscellaneous/multitenancy) — global filter pattern
- [shadcn/ui Vite installation — ui.shadcn.com](https://ui.shadcn.com/docs/installation/vite) — Tailwind v4 + Vite setup
- [Hangfire PostgreSQL — GitHub](https://github.com/hangfire-postgres/Hangfire.PostgreSql) — configuration pattern
- [EFCore.NamingConventions — GitHub](https://github.com/efcore/EFCore.NamingConventions) — UseSnakeCaseNamingConvention

### Tertiary (LOW confidence)
- Build test of `net8.0` target with .NET 9 SDK — confirmed working locally; no official Microsoft guarantee for RC SDK compatibility

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all versions verified against live registries 2026-04-12
- Architecture: HIGH — patterns directly from AGENTS.md (project documentation) + Microsoft docs
- Multi-tenancy pattern: HIGH — documented in AGENTS.md with code examples; aligned with Microsoft EF Core docs
- Pitfalls: MEDIUM — based on known .NET ecosystem patterns; A1-A6 assumptions flagged
- Environment: HIGH — directly probed on target machine

**Research date:** 2026-04-12
**Valid until:** 2026-07-12 (90 days — stable APIs; shorter validity if React 19 or .NET releases major updates)

---

## RESEARCH COMPLETE
