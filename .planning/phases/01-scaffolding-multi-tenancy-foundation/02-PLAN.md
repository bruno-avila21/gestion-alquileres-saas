---
id: 1-02
title: Domain Entities + EF Core Multi-tenancy + Initial Migration
wave: 2
depends_on: [1-01]
files_modified:
  - api/src/GestionAlquileres.Domain/Entities/Organization.cs
  - api/src/GestionAlquileres.Domain/Entities/User.cs
  - api/src/GestionAlquileres.Domain/Entities/ITenantEntity.cs
  - api/src/GestionAlquileres.Domain/Enums/UserRole.cs
  - api/src/GestionAlquileres.Domain/Interfaces/Services/ICurrentTenant.cs
  - api/src/GestionAlquileres.Domain/Interfaces/Repositories/IOrganizationRepository.cs
  - api/src/GestionAlquileres.Domain/Interfaces/Repositories/IUserRepository.cs
  - api/src/GestionAlquileres.Infrastructure/Persistence/AppDbContext.cs
  - api/src/GestionAlquileres.Infrastructure/Persistence/Configurations/OrganizationConfiguration.cs
  - api/src/GestionAlquileres.Infrastructure/Persistence/Configurations/UserConfiguration.cs
  - api/src/GestionAlquileres.Infrastructure/Persistence/Repositories/OrganizationRepository.cs
  - api/src/GestionAlquileres.Infrastructure/Persistence/Repositories/UserRepository.cs
  - api/src/GestionAlquileres.Infrastructure/Services/CurrentTenantService.cs
  - api/src/GestionAlquileres.Infrastructure/DependencyInjection.cs
  - api/src/GestionAlquileres.API/Middleware/TenantMiddleware.cs
  - api/src/GestionAlquileres.API/Program.cs
  - api/src/GestionAlquileres.Infrastructure/Persistence/Migrations/
  - api/tests/GestionAlquileres.Tests/TenantIsolationTests.cs
autonomous: true
requirements: [INFRA-01, INFRA-02]
must_haves:
  truths:
    - "Domain project has zero infrastructure references (EF Core, HttpContext, etc.)"
    - "AppDbContext applies HasQueryFilter to User entity filtering by OrganizationId"
    - "Organization entity has NO global filter (it IS the tenant root)"
    - "ICurrentTenant returns Guid.Empty for unauthenticated requests (no throw)"
    - "Initial migration creates organizations and users tables with snake_case naming"
    - "Integration test proves Tenant A cannot see Tenant B's users via DbContext queries"
  artifacts:
    - path: "api/src/GestionAlquileres.Domain/Entities/Organization.cs"
      provides: "Tenant root entity"
    - path: "api/src/GestionAlquileres.Domain/Entities/User.cs"
      provides: "Multi-tenant user with OrganizationId FK and Role"
    - path: "api/src/GestionAlquileres.Domain/Interfaces/Services/ICurrentTenant.cs"
      provides: "Tenant context abstraction used by DbContext"
    - path: "api/src/GestionAlquileres.Infrastructure/Persistence/AppDbContext.cs"
      provides: "DbContext with global query filters"
    - path: "api/src/GestionAlquileres.Infrastructure/Services/CurrentTenantService.cs"
      provides: "ICurrentTenant implementation reading org_id claim from IHttpContextAccessor"
    - path: "api/src/GestionAlquileres.Infrastructure/Persistence/Migrations/"
      provides: "Initial EF Core migration"
  key_links:
    - from: "AppDbContext.OnModelCreating"
      to: "ICurrentTenant.OrganizationId"
      via: "HasQueryFilter closure over _currentTenant"
      pattern: "HasQueryFilter.*OrganizationId.*_currentTenant"
    - from: "CurrentTenantService"
      to: "IHttpContextAccessor"
      via: "User.FindFirstValue(\"org_id\")"
      pattern: "FindFirstValue.*org_id"
    - from: "Program.cs"
      to: "AppDbContext"
      via: "AddDbContext with UseNpgsql + UseSnakeCaseNamingConvention"
      pattern: "UseSnakeCaseNamingConvention"
---

# Plan 1-02: Domain Entities + EF Core Multi-tenancy + Initial Migration

## Objective

Create the pure-Domain entities (Organization, User, ITenantEntity marker, UserRole enum), the ICurrentTenant abstraction, EF Core AppDbContext with global query filters, Infrastructure repositories, and the initial migration. Expose the `org_id` claim via `CurrentTenantService` so that every subsequent tenant-scoped entity auto-filters by `OrganizationId`.

**Purpose:** INFRA-01 and INFRA-02 are THE critical security foundation. A missed filter here = data leak across tenants for the entire product lifetime. This plan establishes the pattern all future entities (Property, Tenant, Contract, RentHistory, Transaction, Document) will follow.

**Output:** A DbContext that `dotnet ef migrations add InitialCreate` generates the `organizations` + `users` tables in snake_case, with a proven integration test that Tenant A cannot read Tenant B's data.

## must_haves

- [ ] `Organization.cs` has `Id`, `Name`, `Slug`, `Plan`, `IsActive`, `CreatedAt` properties
- [ ] `User.cs` has `Id`, `OrganizationId`, `Email`, `PasswordHash`, `FirstName`, `LastName`, `Role` (UserRole enum), `IsActive`, `CreatedAt`
- [ ] `User` implements `ITenantEntity` (exposes `OrganizationId`)
- [ ] `AppDbContext.OnModelCreating` applies `HasQueryFilter` to `User` but NOT to `Organization`
- [ ] `CurrentTenantService` returns `Guid.Empty` when no JWT claim present
- [ ] `dotnet ef migrations add InitialCreate` succeeds and creates `organizations` + `users` snake_case tables
- [ ] `TenantIsolationTests` proves a query through ICurrentTenant(OrgA) does NOT return users of OrgB

## Tasks

<task id="1-02-01">
<title>Task 1: Create Domain entities + enums + interfaces (zero infrastructure deps)</title>
<read_first>
- .planning/ERD-AND-API.md (lines 25-52) — organizations + users schema
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 350-390) — ICurrentTenant interface contract
- CLAUDE.md — Domain layer rules: NO EF Core, NO HTTP, NO infrastructure references
- api/src/GestionAlquileres.Domain/GestionAlquileres.Domain.csproj — verify zero infra deps before adding code
</read_first>
<action>
Create these files inside `api/src/GestionAlquileres.Domain/`:

**`Enums/UserRole.cs`:**
```csharp
namespace GestionAlquileres.Domain.Enums;

public enum UserRole
{
    Admin = 1,
    Staff = 2,
    Tenant = 3
}
```

**`Entities/ITenantEntity.cs`:**
```csharp
namespace GestionAlquileres.Domain.Entities;

/// <summary>
/// Marker interface for entities scoped to an Organization (tenant).
/// AppDbContext automatically applies OrganizationId HasQueryFilter to these.
/// Organization itself does NOT implement this — it IS the tenant root.
/// </summary>
public interface ITenantEntity
{
    Guid OrganizationId { get; }
}
```

**`Entities/Organization.cs`:**
```csharp
namespace GestionAlquileres.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Plan { get; set; } = "free";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

**`Entities/User.cs`:**
```csharp
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Domain.Entities;

public class User : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

**`Interfaces/Services/ICurrentTenant.cs`:**
```csharp
namespace GestionAlquileres.Domain.Interfaces.Services;

/// <summary>
/// Exposes the current request's OrganizationId.
/// Returns Guid.Empty for unauthenticated/background requests —
/// callers that require a tenant must validate this explicitly.
/// </summary>
public interface ICurrentTenant
{
    Guid OrganizationId { get; }
}
```

**`Interfaces/Repositories/IOrganizationRepository.cs`:**
```csharp
using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Organization?> GetBySlugAsync(string slug, CancellationToken ct);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct);
    Task AddAsync(Organization org, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
```

**`Interfaces/Repositories/IUserRepository.cs`:**
```csharp
using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<User?> GetByEmailAsync(Guid organizationId, string email, CancellationToken ct);
    Task<User?> GetByEmailAcrossOrgsAsync(string email, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
```

Note: `GetByEmailAcrossOrgsAsync` uses `IgnoreQueryFilters()` — it's the only repository method that crosses tenants, needed for login flow where JWT does not yet exist. Its implementation is in Task 2.

Verify Domain project still has zero infrastructure references:
```bash
grep -E "EntityFrameworkCore|AspNetCore|Hangfire|HttpContext" api/src/GestionAlquileres.Domain/GestionAlquileres.Domain.csproj
```
(must return empty)

Run `cd api && dotnet build`. Must exit 0.
</action>
<acceptance_criteria>
- File exists: `api/src/GestionAlquileres.Domain/Entities/Organization.cs` and contains `public string Slug`
- File exists: `api/src/GestionAlquileres.Domain/Entities/User.cs` and contains `public Guid OrganizationId` and contains `: ITenantEntity`
- File exists: `api/src/GestionAlquileres.Domain/Entities/ITenantEntity.cs` and contains `Guid OrganizationId`
- File exists: `api/src/GestionAlquileres.Domain/Enums/UserRole.cs` and contains `Admin = 1` and `Staff = 2` and `Tenant = 3`
- File exists: `api/src/GestionAlquileres.Domain/Interfaces/Services/ICurrentTenant.cs` and contains `Guid OrganizationId`
- File exists: `api/src/GestionAlquileres.Domain/Interfaces/Repositories/IOrganizationRepository.cs`
- File exists: `api/src/GestionAlquileres.Domain/Interfaces/Repositories/IUserRepository.cs` and contains `GetByEmailAcrossOrgsAsync`
- `api/src/GestionAlquileres.Domain/GestionAlquileres.Domain.csproj` has ZERO matches for regex `EntityFrameworkCore|AspNetCore|Hangfire|HttpContext`
- `cd api && dotnet build` exits 0
</acceptance_criteria>
</task>

<task id="1-02-02">
<title>Task 2: AppDbContext + EF configs + CurrentTenantService + repositories + DI wiring</title>
<read_first>
- api/src/GestionAlquileres.Domain/Entities/Organization.cs (from Task 1)
- api/src/GestionAlquileres.Domain/Entities/User.cs (from Task 1)
- api/src/GestionAlquileres.Domain/Interfaces/Services/ICurrentTenant.cs (from Task 1)
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 395-432) — AppDbContext global filter pattern
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 690-727) — snake_case EF config pattern
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 760-775) — unauthenticated request pitfall
- api/src/GestionAlquileres.API/Program.cs (from Plan 01) — add DI wiring here
- CLAUDE.md — multi-tenancy rules
</read_first>
<action>
Create files inside `api/src/GestionAlquileres.Infrastructure/`:

**`Persistence/AppDbContext.cs`:**
```csharp
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ICurrentTenant _currentTenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenant currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Organization is the tenant ROOT — no filter (filtering on its own PK is circular)
        // User and all future ITenantEntity implementations are filtered.
        modelBuilder.Entity<User>()
            .HasQueryFilter(u => u.OrganizationId == _currentTenant.OrganizationId);
    }
}
```

**`Persistence/Configurations/OrganizationConfiguration.cs`:**
```csharp
using GestionAlquileres.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(o => o.Name).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Slug).IsRequired().HasMaxLength(100);
        builder.HasIndex(o => o.Slug).IsUnique();
        builder.Property(o => o.Plan).IsRequired().HasMaxLength(20).HasDefaultValue("free");
        builder.Property(o => o.IsActive).HasDefaultValue(true);
        builder.Property(o => o.CreatedAt).HasDefaultValueSql("now()");
    }
}
```

**`Persistence/Configurations/UserConfiguration.cs`:**
```csharp
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(u => u.OrganizationId).IsRequired();
        builder.Property(u => u.Email).IsRequired().HasMaxLength(320);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Role).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(u => new { u.OrganizationId, u.Email }).IsUnique();
        builder.HasIndex(u => u.OrganizationId);
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(u => u.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

**`Services/CurrentTenantService.cs`:**
```csharp
using System.Security.Claims;
using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace GestionAlquileres.Infrastructure.Services;

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
            var claim = _httpContextAccessor.HttpContext?.User.FindFirstValue("org_id");
            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }
    }
}
```

**`Persistence/Repositories/OrganizationRepository.cs`:**
```csharp
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly AppDbContext _db;
    public OrganizationRepository(AppDbContext db) => _db = db;

    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Organizations.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Organization?> GetBySlugAsync(string slug, CancellationToken ct) =>
        _db.Organizations.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Slug == slug, ct);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct) =>
        _db.Organizations.IgnoreQueryFilters().AnyAsync(o => o.Slug == slug, ct);

    public async Task AddAsync(Organization org, CancellationToken ct)
    {
        await _db.Organizations.AddAsync(org, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
```

Note: `Organization` has no global filter in `OnModelCreating`, so `IgnoreQueryFilters()` here is defensive but not strictly required. Kept for intent clarity (we're crossing tenants by design in auth flows).

**`Persistence/Repositories/UserRepository.cs`:**
```csharp
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(Guid organizationId, string email, CancellationToken ct) =>
        _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.OrganizationId == organizationId && u.Email == email, ct);

    public Task<User?> GetByEmailAcrossOrgsAsync(string email, CancellationToken ct) =>
        _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct)
    {
        await _db.Users.AddAsync(user, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
```

**`DependencyInjection.cs` (Infrastructure project root):**
```csharp
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using GestionAlquileres.Infrastructure.Persistence;
using GestionAlquileres.Infrastructure.Persistence.Repositories;
using GestionAlquileres.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestionAlquileres.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection missing");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<ICurrentTenant, CurrentTenantService>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
```

**`api/src/GestionAlquileres.API/Middleware/TenantMiddleware.cs`:**
```csharp
namespace GestionAlquileres.API.Middleware;

/// <summary>
/// Placeholder middleware. In current design the org_id claim is extracted on
/// demand by CurrentTenantService via IHttpContextAccessor — no imperative
/// enrichment needed. This middleware exists as an explicit extension point for
/// future tenant-resolution strategies (subdomain routing, header-based, etc.).
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    public TenantMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context) => _next(context);
}
```

**Update `api/src/GestionAlquileres.API/Program.cs`** — insert after `AddHttpContextAccessor()`:

```csharp
// Infrastructure: DbContext, ICurrentTenant, repositories
builder.Services.AddInfrastructure(builder.Configuration);
```

Add using directive at top: `using GestionAlquileres.Infrastructure;`

Also insert before `app.UseAuthorization();`:
```csharp
app.UseMiddleware<GestionAlquileres.API.Middleware.TenantMiddleware>();
```

Run `cd api && dotnet build`. Must exit 0.
</action>
<acceptance_criteria>
- File exists: `api/src/GestionAlquileres.Infrastructure/Persistence/AppDbContext.cs` and contains `HasQueryFilter(u => u.OrganizationId == _currentTenant.OrganizationId)`
- `AppDbContext.cs` does NOT contain `HasQueryFilter` for `Organization` (only for `User`)
- File exists: `api/src/GestionAlquileres.Infrastructure/Persistence/Configurations/OrganizationConfiguration.cs` and contains `ToTable("organizations")`
- File exists: `api/src/GestionAlquileres.Infrastructure/Persistence/Configurations/UserConfiguration.cs` and contains `ToTable("users")` and `HasIndex(u => new { u.OrganizationId, u.Email }).IsUnique()`
- File exists: `api/src/GestionAlquileres.Infrastructure/Services/CurrentTenantService.cs` and contains `FindFirstValue("org_id")` and `Guid.Empty`
- File exists: `api/src/GestionAlquileres.Infrastructure/Persistence/Repositories/OrganizationRepository.cs`
- File exists: `api/src/GestionAlquileres.Infrastructure/Persistence/Repositories/UserRepository.cs` and contains `IgnoreQueryFilters` in `GetByEmailAcrossOrgsAsync`
- File exists: `api/src/GestionAlquileres.Infrastructure/DependencyInjection.cs` and contains `UseSnakeCaseNamingConvention` and `AddScoped<ICurrentTenant, CurrentTenantService>`
- File exists: `api/src/GestionAlquileres.API/Middleware/TenantMiddleware.cs`
- `api/src/GestionAlquileres.API/Program.cs` contains `AddInfrastructure(builder.Configuration)`
- `api/src/GestionAlquileres.API/Program.cs` contains `UseMiddleware<GestionAlquileres.API.Middleware.TenantMiddleware>`
- `cd api && dotnet build` exits 0
</acceptance_criteria>
</task>

<task id="1-02-03">
<title>Task 3: Generate initial EF migration + write tenant isolation integration test</title>
<read_first>
- api/src/GestionAlquileres.Infrastructure/Persistence/AppDbContext.cs (from Task 2)
- api/src/GestionAlquileres.Infrastructure/DependencyInjection.cs (from Task 2)
- api/src/GestionAlquileres.API/appsettings.json (from Plan 01) — connection string
- api/tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj (from Plan 01) — add test packages here
- CLAUDE.md — migration command syntax
</read_first>
<action>
**Step 1 — Ensure Docker is running.** From repo root:
```bash
docker compose up -d postgres
```
Wait for healthcheck (about 10s). Verify with `docker ps` that `gestion_alquileres_pg` is `(healthy)`.

**Step 2 — Generate initial migration.** From `api/`:
```bash
dotnet tool install --global dotnet-ef --version 8.0.11
```
(skip if already installed; use `dotnet tool update --global dotnet-ef --version 8.0.11` if wrong version present)

```bash
dotnet ef migrations add InitialCreate \
  --project src/GestionAlquileres.Infrastructure \
  --startup-project src/GestionAlquileres.API \
  --output-dir Persistence/Migrations
```

Verify file exists at `api/src/GestionAlquileres.Infrastructure/Persistence/Migrations/` — should contain a `*_InitialCreate.cs`, a `*_InitialCreate.Designer.cs`, and `AppDbContextModelSnapshot.cs`.

Grep the migration: must contain `.ToTable("organizations"` or `migrationBuilder.CreateTable(\n                name: "organizations"` — snake_case plural table names.

**Step 3 — Apply migration to local DB:**
```bash
dotnet ef database update \
  --project src/GestionAlquileres.Infrastructure \
  --startup-project src/GestionAlquileres.API
```

**Step 4 — Add test packages** to `api/tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj`:
```bash
dotnet add tests/GestionAlquileres.Tests package Microsoft.EntityFrameworkCore.InMemory --version 8.0.11
dotnet add tests/GestionAlquileres.Tests package Microsoft.AspNetCore.Mvc.Testing --version 8.0.11
```

**Step 5 — Create `api/tests/GestionAlquileres.Tests/TenantIsolationTests.cs`:**

```csharp
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Services;
using GestionAlquileres.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Tests;

public class TenantIsolationTests
{
    private sealed class StubCurrentTenant : ICurrentTenant
    {
        public Guid OrganizationId { get; set; }
    }

    private static AppDbContext NewContext(StubCurrentTenant tenant, string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options, tenant);
    }

    [Fact]
    public async Task User_query_returns_only_current_tenant_rows()
    {
        var dbName = Guid.NewGuid().ToString();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        // Seed with global-filter bypass (via IgnoreQueryFilters on Add is N/A;
        // we insert from an unscoped tenant context using Guid.Empty that still
        // hits the filter but Add() is not filtered).
        var seedTenant = new StubCurrentTenant { OrganizationId = Guid.Empty };
        using (var seed = NewContext(seedTenant, dbName))
        {
            seed.Users.Add(new User { OrganizationId = orgA, Email = "a@a.com", FirstName="A", LastName="A", PasswordHash="x", Role = UserRole.Admin });
            seed.Users.Add(new User { OrganizationId = orgB, Email = "b@b.com", FirstName="B", LastName="B", PasswordHash="x", Role = UserRole.Admin });
            await seed.SaveChangesAsync();
        }

        // Query as Tenant A — must see only orgA user
        var tenantA = new StubCurrentTenant { OrganizationId = orgA };
        using var ctxA = NewContext(tenantA, dbName);
        var visibleToA = await ctxA.Users.ToListAsync();

        Assert.Single(visibleToA);
        Assert.Equal(orgA, visibleToA[0].OrganizationId);
        Assert.DoesNotContain(visibleToA, u => u.OrganizationId == orgB);
    }

    [Fact]
    public async Task User_query_returns_empty_when_tenant_is_empty()
    {
        var dbName = Guid.NewGuid().ToString();
        var orgA = Guid.NewGuid();

        var seedTenant = new StubCurrentTenant { OrganizationId = Guid.Empty };
        using (var seed = NewContext(seedTenant, dbName))
        {
            seed.Users.Add(new User { OrganizationId = orgA, Email = "a@a.com", FirstName="A", LastName="A", PasswordHash="x", Role = UserRole.Admin });
            await seed.SaveChangesAsync();
        }

        // Unauthenticated (Guid.Empty) → filter matches nothing
        var emptyTenant = new StubCurrentTenant { OrganizationId = Guid.Empty };
        using var ctx = NewContext(emptyTenant, dbName);
        var visible = await ctx.Users.ToListAsync();

        Assert.Empty(visible);
    }

    [Fact]
    public void Organization_entity_has_no_query_filter()
    {
        var tenant = new StubCurrentTenant { OrganizationId = Guid.NewGuid() };
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var ctx = new AppDbContext(options, tenant);

        var orgEntityType = ctx.Model.FindEntityType(typeof(Organization));
        Assert.NotNull(orgEntityType);
        // EF Core 8: GetQueryFilter() returns null when none configured
        Assert.Null(orgEntityType!.GetQueryFilter());

        var userEntityType = ctx.Model.FindEntityType(typeof(User));
        Assert.NotNull(userEntityType!.GetQueryFilter());
    }
}
```

**Step 6 — Run tests:**
```bash
cd api && dotnet test
```
All tests in `TenantIsolationTests` + `SmokeTests` must pass. Exit 0.
</action>
<acceptance_criteria>
- Directory exists: `api/src/GestionAlquileres.Infrastructure/Persistence/Migrations/`
- A file matching `api/src/GestionAlquileres.Infrastructure/Persistence/Migrations/*_InitialCreate.cs` exists
- `api/src/GestionAlquileres.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs` exists
- The `*_InitialCreate.cs` migration file contains the string `"organizations"` (snake_case plural)
- The `*_InitialCreate.cs` migration file contains the string `"users"` (snake_case plural)
- The `*_InitialCreate.cs` migration file contains `"organization_id"` (snake_case column name)
- File exists: `api/tests/GestionAlquileres.Tests/TenantIsolationTests.cs`
- `api/tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj` contains `<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory"`
- `cd api && dotnet build` exits 0
- `cd api && dotnet test` exits 0
- `cd api && dotnet test --filter "FullyQualifiedName~TenantIsolationTests"` exits 0 and reports 3 passed tests
</acceptance_criteria>
</task>

## Verification

- `cd api && dotnet build` exits 0
- `cd api && dotnet test` exits 0
- Grep `api/src/GestionAlquileres.Domain/GestionAlquileres.Domain.csproj` for `EntityFrameworkCore|AspNetCore|Hangfire|HttpContext` returns ZERO matches
- `api/src/GestionAlquileres.Infrastructure/Persistence/AppDbContext.cs` contains `HasQueryFilter` exactly once (for User)
- `dotnet ef migrations list --project src/GestionAlquileres.Infrastructure --startup-project src/GestionAlquileres.API` (from `api/`) shows `InitialCreate` applied

## Threat Model

| ID | Threat | Category | Mitigation | ASVS |
|----|--------|----------|------------|------|
| T-1-05 | Tenant data leakage via missed WHERE clause | Information Disclosure | EF Core global `HasQueryFilter` on `User` + `ITenantEntity` marker + policy of adding filter per entity in `OnModelCreating` | V4, V8 |
| T-1-06 | OrganizationId spoofing via request body | Elevation of Privilege | `ICurrentTenant` reads ONLY from `org_id` JWT claim; no command/DTO in this plan accepts `OrganizationId` from body | V4 |
| T-1-07 | Unauthenticated request returns empty list (silent data hiding instead of 401) | Information Disclosure | `CurrentTenantService` returns `Guid.Empty` making filter match nothing; documented that ALL tenant endpoints require `[Authorize]` attribute (enforced in Plan 03); integration test proves filter behavior | V4 |
| T-1-08 | `IgnoreQueryFilters()` overused, bypassing tenant isolation | Information Disclosure | Only repository methods explicitly documented as cross-tenant (login flow) use it; code review checklist includes grep for `IgnoreQueryFilters` | V4 |
| T-1-09 | Circular filter on Organization entity returning empty orgs list | Denial of Service / Information Disclosure | Organization entity has NO `HasQueryFilter`; verified by `Organization_entity_has_no_query_filter` test | V4 |
