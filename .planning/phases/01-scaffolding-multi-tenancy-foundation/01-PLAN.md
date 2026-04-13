---
id: 1-01
title: .NET 8 Solution Scaffold + Core Infrastructure
wave: 1
depends_on: []
files_modified:
  - api/global.json
  - api/GestionAlquileres.sln
  - api/src/GestionAlquileres.Domain/GestionAlquileres.Domain.csproj
  - api/src/GestionAlquileres.Application/GestionAlquileres.Application.csproj
  - api/src/GestionAlquileres.Infrastructure/GestionAlquileres.Infrastructure.csproj
  - api/src/GestionAlquileres.API/GestionAlquileres.API.csproj
  - api/src/GestionAlquileres.API/Program.cs
  - api/src/GestionAlquileres.API/appsettings.json
  - api/src/GestionAlquileres.API/appsettings.Development.json
  - api/tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj
  - api/tests/GestionAlquileres.Tests/SmokeTests.cs
  - docker-compose.yml
  - .gitignore
autonomous: true
requirements: [INFRA-03, INFRA-04, INFRA-05]
must_haves:
  truths:
    - "Solution builds cleanly targeting net8.0"
    - "Docker Compose starts PostgreSQL + MinIO + Hangfire dependencies"
    - "Serilog writes structured logs to console and rolling file"
    - "Hangfire dashboard is reachable at /hangfire in Development"
  artifacts:
    - path: "api/GestionAlquileres.sln"
      provides: "4-project Clean Architecture solution"
    - path: "api/global.json"
      provides: "SDK version pin to 9.0.304"
    - path: "docker-compose.yml"
      provides: "Local dev infra: PostgreSQL 16 + MinIO"
    - path: "api/src/GestionAlquileres.API/Program.cs"
      provides: "Host bootstrap with Serilog + Hangfire + Swagger"
  key_links:
    - from: "Program.cs"
      to: "Hangfire.PostgreSql"
      via: "UsePostgreSqlStorage(connectionString)"
      pattern: "UsePostgreSqlStorage"
    - from: "Program.cs"
      to: "Serilog"
      via: "UseSerilog with Console+File sinks"
      pattern: "UseSerilog"
---

# Plan 1-01: .NET 8 Solution Scaffold + Core Infrastructure

## Objective

Create the 4-project .NET 8 Clean Architecture solution, pin SDK version, install all NuGet packages with the versions mandated by research, configure Docker Compose for local PostgreSQL + MinIO, wire Serilog structured logging and Hangfire job scheduling. This is the skeleton all other plans build on.

**Purpose:** Everything downstream (EF Core, MediatR, JWT, Hangfire jobs) depends on this skeleton existing with correct package versions. Version drift here propagates to all 8 phases.

**Output:** A solution that `dotnet build` compiles cleanly, with Serilog and Hangfire configured but no entities/endpoints yet (those land in Plans 02 and 03).

## must_haves

- [ ] `api/GestionAlquileres.sln` lists 4 projects + 1 test project
- [ ] `api/global.json` pins SDK to `9.0.304` with `rollForward: latestPatch`
- [ ] All 4 `.csproj` files target `net8.0`
- [ ] `dotnet build` in `api/` exits 0
- [ ] `docker-compose.yml` at repo root defines `postgres` (port 5432) and `minio` (ports 9000/9001)
- [ ] `docker compose up -d` starts containers successfully (verified by `docker ps`)
- [ ] `appsettings.json` contains `ConnectionStrings.DefaultConnection`, `ConnectionStrings.HangfireConnection`, `JwtSettings`, `Serilog` sections
- [ ] `Program.cs` calls `UseSerilog(...)` and `AddHangfire(...)` with PostgreSQL storage
- [ ] `GestionAlquileres.Tests.csproj` exists with xUnit package reference and runs `dotnet test` exit 0

## Tasks

<task id="1-01-01">
<title>Task 1: Create Wave 0 test project scaffold (xUnit)</title>
<read_first>
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-VALIDATION.md — Wave 0 requirements list
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 1060-1115) — validation architecture and Wave 0 gaps
</read_first>
<action>
Create the test project skeleton BEFORE any other code so that downstream tasks have a place to land tests.

Working directory: `C:/Users/Bruno Avila/Documents/Proyectos_Propios/gestion-alquileres-saas/`

Run from repo root (Bash/PowerShell compatible — use forward slashes):

```bash
cd api
dotnet new sln -n GestionAlquileres
mkdir -p tests
cd tests
dotnet new xunit -n GestionAlquileres.Tests --framework net8.0
cd ..
dotnet sln add tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj
```

Then create `api/tests/GestionAlquileres.Tests/SmokeTests.cs` with exactly this content:

```csharp
namespace GestionAlquileres.Tests;

public class SmokeTests
{
    [Fact]
    public void Sanity_TestInfrastructureAlive()
    {
        Assert.True(true);
    }
}
```

Delete the auto-generated `UnitTest1.cs` file that `dotnet new xunit` creates.
</action>
<acceptance_criteria>
- File exists: `api/GestionAlquileres.sln`
- File exists: `api/tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj`
- File exists: `api/tests/GestionAlquileres.Tests/SmokeTests.cs`
- File does NOT exist: `api/tests/GestionAlquileres.Tests/UnitTest1.cs`
- `api/tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj` contains `<TargetFramework>net8.0</TargetFramework>`
- `api/tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj` contains `xunit` package reference
- `cd api && dotnet test` exits 0 (runs the Sanity smoke test)
</acceptance_criteria>
</task>

<task id="1-01-02">
<title>Task 2: Scaffold 4 Clean Architecture projects + global.json + project references</title>
<read_first>
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 170-225) — exact installation commands and versions
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 800-815) — global.json content
- CLAUDE.md — Clean Architecture layering rules (Domain references nothing; Application references Domain only; Infrastructure references Application; API references all)
- api/GestionAlquileres.sln (from Task 1)
</read_first>
<action>
Create `api/global.json` with exactly this content:

```json
{
  "sdk": {
    "version": "9.0.304",
    "rollForward": "latestPatch"
  }
}
```

From `api/` directory, scaffold projects:

```bash
dotnet new classlib -n GestionAlquileres.Domain -o src/GestionAlquileres.Domain --framework net8.0
dotnet new classlib -n GestionAlquileres.Application -o src/GestionAlquileres.Application --framework net8.0
dotnet new classlib -n GestionAlquileres.Infrastructure -o src/GestionAlquileres.Infrastructure --framework net8.0
dotnet new webapi -n GestionAlquileres.API -o src/GestionAlquileres.API --framework net8.0 --use-controllers

dotnet sln add src/GestionAlquileres.Domain/GestionAlquileres.Domain.csproj
dotnet sln add src/GestionAlquileres.Application/GestionAlquileres.Application.csproj
dotnet sln add src/GestionAlquileres.Infrastructure/GestionAlquileres.Infrastructure.csproj
dotnet sln add src/GestionAlquileres.API/GestionAlquileres.API.csproj
```

Delete the default `Class1.cs` in each classlib (Domain, Application, Infrastructure).
Delete `WeatherForecast.cs` and `Controllers/WeatherForecastController.cs` from the API project.

Add project references (Clean Architecture topology — STRICT):

```bash
dotnet add src/GestionAlquileres.Application/GestionAlquileres.Application.csproj reference src/GestionAlquileres.Domain/GestionAlquileres.Domain.csproj
dotnet add src/GestionAlquileres.Infrastructure/GestionAlquileres.Infrastructure.csproj reference src/GestionAlquileres.Application/GestionAlquileres.Application.csproj
dotnet add src/GestionAlquileres.API/GestionAlquileres.API.csproj reference src/GestionAlquileres.Infrastructure/GestionAlquileres.Infrastructure.csproj
dotnet add src/GestionAlquileres.API/GestionAlquileres.API.csproj reference src/GestionAlquileres.Application/GestionAlquileres.Application.csproj

dotnet add tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj reference src/GestionAlquileres.API/GestionAlquileres.API.csproj
dotnet add tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj reference src/GestionAlquileres.Infrastructure/GestionAlquileres.Infrastructure.csproj
dotnet add tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj reference src/GestionAlquileres.Application/GestionAlquileres.Application.csproj
dotnet add tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj reference src/GestionAlquileres.Domain/GestionAlquileres.Domain.csproj
```

Domain MUST have zero project references. Infrastructure MUST NOT reference API. Do NOT add Microsoft.AspNetCore.App FrameworkReference to Domain or Application.

Run `dotnet build` from `api/` — must exit 0.
</action>
<acceptance_criteria>
- File exists: `api/global.json` containing `"version": "9.0.304"`
- File exists: `api/src/GestionAlquileres.Domain/GestionAlquileres.Domain.csproj`
- File exists: `api/src/GestionAlquileres.Application/GestionAlquileres.Application.csproj`
- File exists: `api/src/GestionAlquileres.Infrastructure/GestionAlquileres.Infrastructure.csproj`
- File exists: `api/src/GestionAlquileres.API/GestionAlquileres.API.csproj`
- Each `.csproj` contains `<TargetFramework>net8.0</TargetFramework>`
- `api/src/GestionAlquileres.Domain/GestionAlquileres.Domain.csproj` contains ZERO `<ProjectReference` or `<PackageReference` to EF Core, AspNetCore, Hangfire, HttpClient
- `api/src/GestionAlquileres.Application/GestionAlquileres.Application.csproj` contains `<ProjectReference Include="..\GestionAlquileres.Domain\GestionAlquileres.Domain.csproj"`
- `api/src/GestionAlquileres.Infrastructure/GestionAlquileres.Infrastructure.csproj` contains `<ProjectReference Include="..\GestionAlquileres.Application\GestionAlquileres.Application.csproj"`
- `api/src/GestionAlquileres.API/GestionAlquileres.API.csproj` contains ProjectReference to Infrastructure AND Application
- File does NOT exist: `api/src/GestionAlquileres.API/WeatherForecast.cs`
- File does NOT exist: `api/src/GestionAlquileres.API/Controllers/WeatherForecastController.cs`
- `cd api && dotnet build` exits 0
</acceptance_criteria>
</task>

<task id="1-01-03">
<title>Task 3: Install all NuGet packages at research-verified versions</title>
<read_first>
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 95-210) — exact package names and versions
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 729-738) — anti-patterns (do NOT install MediatR.Extensions.Microsoft.DependencyInjection or AutoMapper.Extensions.Microsoft.DependencyInjection)
- api/src/GestionAlquileres.Infrastructure/GestionAlquileres.Infrastructure.csproj
- api/src/GestionAlquileres.Application/GestionAlquileres.Application.csproj
- api/src/GestionAlquileres.API/GestionAlquileres.API.csproj
</read_first>
<action>
Run from `api/` directory. Install packages at EXACTLY these versions (do not use `latest`):

**Infrastructure project:**
```bash
dotnet add src/GestionAlquileres.Infrastructure package Microsoft.EntityFrameworkCore --version 8.0.11
dotnet add src/GestionAlquileres.Infrastructure package Microsoft.EntityFrameworkCore.Design --version 8.0.11
dotnet add src/GestionAlquileres.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.11
dotnet add src/GestionAlquileres.Infrastructure package Microsoft.EntityFrameworkCore.Tools --version 8.0.11
dotnet add src/GestionAlquileres.Infrastructure package EFCore.NamingConventions --version 8.0.3
dotnet add src/GestionAlquileres.Infrastructure package Hangfire.Core --version 1.8.23
dotnet add src/GestionAlquileres.Infrastructure package Hangfire.AspNetCore --version 1.8.23
dotnet add src/GestionAlquileres.Infrastructure package Hangfire.PostgreSql --version 1.21.1
dotnet add src/GestionAlquileres.Infrastructure package AutoMapper --version 16.1.1
dotnet add src/GestionAlquileres.Infrastructure package BCrypt.Net-Next --version 4.0.3
dotnet add src/GestionAlquileres.Infrastructure package System.IdentityModel.Tokens.Jwt --version 8.17.0
dotnet add src/GestionAlquileres.Infrastructure package Microsoft.AspNetCore.Http.Abstractions --version 2.3.0
dotnet add src/GestionAlquileres.Infrastructure package Microsoft.Extensions.Configuration.Abstractions --version 8.0.0
dotnet add src/GestionAlquileres.Infrastructure package Microsoft.Extensions.DependencyInjection.Abstractions --version 8.0.2
dotnet add src/GestionAlquileres.Infrastructure package Microsoft.Extensions.Options.ConfigurationExtensions --version 8.0.0
```

Note: BCrypt.Net-Next latest is 4.0.3 on NuGet (research said 4.1.0 — if 4.1.0 does not resolve, use 4.0.3). If the restore fails for a version, use the closest stable version available.

**Application project:**
```bash
dotnet add src/GestionAlquileres.Application package MediatR --version 12.4.1
dotnet add src/GestionAlquileres.Application package FluentValidation --version 11.11.0
dotnet add src/GestionAlquileres.Application package FluentValidation.DependencyInjectionExtensions --version 11.11.0
dotnet add src/GestionAlquileres.Application package AutoMapper --version 13.0.1
dotnet add src/GestionAlquileres.Application package Microsoft.Extensions.Logging.Abstractions --version 8.0.2
dotnet add src/GestionAlquileres.Application package Microsoft.Extensions.DependencyInjection.Abstractions --version 8.0.2
```

Note on versions: research recommends MediatR 14.1.0 + FluentValidation 12.1.1 + AutoMapper 16.1.1. Those versions target net8.0+ but may have compatibility issues. Use stable versions known to target net8.0: MediatR 12.4.1, FluentValidation 11.11.0, AutoMapper 13.0.1. If executor finds newer versions that resolve cleanly with net8.0, they may use them — otherwise fall back to these. Record the final versions chosen in the SUMMARY.

Do NOT install these packages (they are merged into core since v12/v13):
- `MediatR.Extensions.Microsoft.DependencyInjection`
- `AutoMapper.Extensions.Microsoft.DependencyInjection`

**API project:**
```bash
dotnet add src/GestionAlquileres.API package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.11
dotnet add src/GestionAlquileres.API package Serilog.AspNetCore --version 8.0.3
dotnet add src/GestionAlquileres.API package Serilog.Sinks.Console --version 6.0.0
dotnet add src/GestionAlquileres.API package Serilog.Sinks.File --version 6.0.0
dotnet add src/GestionAlquileres.API package Serilog.Enrichers.Environment --version 3.0.1
dotnet add src/GestionAlquileres.API package Swashbuckle.AspNetCore --version 7.2.0
```

Note: research recommended Serilog.AspNetCore 10.0.0 — that version requires .NET 9+. For net8.0 target, use 8.0.3.

After all installs, run `dotnet restore && dotnet build` from `api/`. Must exit 0.
</action>
<acceptance_criteria>
- `api/src/GestionAlquileres.Infrastructure/GestionAlquileres.Infrastructure.csproj` contains `<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL"`
- `api/src/GestionAlquileres.Infrastructure/GestionAlquileres.Infrastructure.csproj` contains `<PackageReference Include="EFCore.NamingConventions"`
- `api/src/GestionAlquileres.Infrastructure/GestionAlquileres.Infrastructure.csproj` contains `<PackageReference Include="Hangfire.PostgreSql"`
- `api/src/GestionAlquileres.Infrastructure/GestionAlquileres.Infrastructure.csproj` contains `<PackageReference Include="BCrypt.Net-Next"`
- `api/src/GestionAlquileres.Application/GestionAlquileres.Application.csproj` contains `<PackageReference Include="MediatR"`
- `api/src/GestionAlquileres.Application/GestionAlquileres.Application.csproj` contains `<PackageReference Include="FluentValidation"`
- `api/src/GestionAlquileres.Application/GestionAlquileres.Application.csproj` does NOT contain `MediatR.Extensions.Microsoft.DependencyInjection`
- `api/src/GestionAlquileres.API/GestionAlquileres.API.csproj` contains `<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer"`
- `api/src/GestionAlquileres.API/GestionAlquileres.API.csproj` contains `<PackageReference Include="Serilog.AspNetCore"`
- `api/src/GestionAlquileres.API/GestionAlquileres.API.csproj` contains `<PackageReference Include="Swashbuckle.AspNetCore"`
- `cd api && dotnet restore` exits 0
- `cd api && dotnet build` exits 0
</acceptance_criteria>
</task>

<task id="1-01-04">
<title>Task 4: Configure Docker Compose + appsettings + Serilog + Hangfire in Program.cs</title>
<read_first>
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 582-622) — Serilog v10 and Hangfire PostgreSQL setup patterns
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 955-986) — Docker Compose template
- api/src/GestionAlquileres.API/Program.cs — current Program.cs (replace entirely)
- api/src/GestionAlquileres.API/appsettings.json — default template (replace)
- CLAUDE.md — JWT secret MUST NOT be hardcoded, read from config
</read_first>
<action>
**Step 1 — Create `docker-compose.yml` at repo root** (`C:/Users/Bruno Avila/Documents/Proyectos_Propios/gestion-alquileres-saas/docker-compose.yml`):

```yaml
services:
  postgres:
    image: postgres:16-alpine
    container_name: gestion_alquileres_pg
    environment:
      POSTGRES_DB: gestion_alquileres
      POSTGRES_USER: appuser
      POSTGRES_PASSWORD: devpassword
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U appuser -d gestion_alquileres"]
      interval: 5s
      timeout: 3s
      retries: 10

  minio:
    image: minio/minio:latest
    container_name: gestion_alquileres_minio
    command: server /data --console-address ":9001"
    environment:
      MINIO_ROOT_USER: minioadmin
      MINIO_ROOT_PASSWORD: minioadmin
    ports:
      - "9000:9000"
      - "9001:9001"
    volumes:
      - minio_data:/data

volumes:
  postgres_data:
  minio_data:
```

**Step 2 — Create/update `.gitignore` at repo root** adding these lines (append, do not truncate existing content):

```
# .NET
bin/
obj/
*.user
.vs/
appsettings.Development.json
logs/

# Node
node_modules/
dist/
.vite/

# Docker volumes (not tracked; just defensive)
postgres_data/
minio_data/
```

**Step 3 — Replace `api/src/GestionAlquileres.API/appsettings.json`** with:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=gestion_alquileres;Username=appuser;Password=devpassword",
    "HangfireConnection": "Host=localhost;Port=5432;Database=gestion_alquileres;Username=appuser;Password=devpassword"
  },
  "JwtSettings": {
    "Issuer": "GestionAlquileres",
    "Audience": "GestionAlquileresClients",
    "SecretKey": "REPLACE_WITH_ENV_VAR_IN_PRODUCTION_MIN_32_CHARS_FOR_HS256",
    "ExpiryHours": 8
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "Hangfire": "Information"
      }
    },
    "Enrich": ["FromLogContext", "WithMachineName"]
  },
  "AllowedHosts": "*"
}
```

Create `api/src/GestionAlquileres.API/appsettings.Development.json` with overrides (Serilog Debug level + local connection). Reuse same structure but `"MinimumLevel.Default": "Debug"`.

**Step 4 — Replace `api/src/GestionAlquileres.API/Program.cs`** entirely with:

```csharp
using Hangfire;
using Hangfire.PostgreSql;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog — structured logging (INFRA-05)
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

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HttpContextAccessor (required by ICurrentTenant in Plan 1-02)
builder.Services.AddHttpContextAccessor();

// Hangfire (INFRA-04) — PostgreSQL storage
var hangfireConn = builder.Configuration.GetConnectionString("HangfireConnection")
    ?? throw new InvalidOperationException("HangfireConnection missing");
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(hangfireConn)));
builder.Services.AddHangfireServer();

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// Hangfire dashboard — read-only outside Development
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    IsReadOnlyFunc = _ => !app.Environment.IsDevelopment()
});

// Health probe — lets tests verify Program.cs bootstraps
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program { } // for WebApplicationFactory<Program> in tests
```

**Step 5 — Replace `api/src/GestionAlquileres.API/Properties/launchSettings.json`** to set profile URL to `http://localhost:5000`:

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

**Step 6 — Verify:** from `api/` run `dotnet build`. Must exit 0.
</action>
<acceptance_criteria>
- File exists: `docker-compose.yml` at repo root
- `docker-compose.yml` contains `image: postgres:16-alpine`
- `docker-compose.yml` contains `image: minio/minio:latest`
- `docker-compose.yml` contains `POSTGRES_DB: gestion_alquileres`
- File exists: `api/src/GestionAlquileres.API/appsettings.json`
- `api/src/GestionAlquileres.API/appsettings.json` contains `"HangfireConnection"`
- `api/src/GestionAlquileres.API/appsettings.json` contains `"JwtSettings"`
- `api/src/GestionAlquileres.API/appsettings.json` contains `"Serilog"`
- `api/src/GestionAlquileres.API/Program.cs` contains `UseSerilog`
- `api/src/GestionAlquileres.API/Program.cs` contains `AddHangfire`
- `api/src/GestionAlquileres.API/Program.cs` contains `UsePostgreSqlStorage`
- `api/src/GestionAlquileres.API/Program.cs` contains `UseHangfireDashboard("/hangfire"`
- `api/src/GestionAlquileres.API/Program.cs` contains `AddHttpContextAccessor`
- `api/src/GestionAlquileres.API/Program.cs` contains `public partial class Program`
- `api/src/GestionAlquileres.API/Program.cs` contains `MapGet("/health"`
- `.gitignore` at repo root contains `bin/` and `obj/` and `logs/`
- `cd api && dotnet build` exits 0
- `cd api && dotnet test` exits 0 (smoke test still passes)
</acceptance_criteria>
</task>

## Verification

- `cd api && dotnet build` exits 0
- `cd api && dotnet test` exits 0
- `docker compose config` exits 0 (validates docker-compose.yml syntax)
- Grep `api/src/GestionAlquileres.Domain/GestionAlquileres.Domain.csproj` returns zero matches for `EntityFrameworkCore|AspNetCore|Hangfire`
- All 4 `.csproj` files in `api/src/` contain `<TargetFramework>net8.0</TargetFramework>`

## Threat Model

| ID | Threat | Category | Mitigation | ASVS |
|----|--------|----------|------------|------|
| T-1-01 | JWT signing key hardcoded in repo | Information Disclosure | `JwtSettings.SecretKey` read from configuration; `appsettings.Development.json` gitignored; Plan 03 enforces env var override for production | V6 |
| T-1-02 | Hangfire dashboard exposed in production | Information Disclosure | `UseHangfireDashboard` with `IsReadOnlyFunc = _ => !app.Environment.IsDevelopment()` | V4 |
| T-1-03 | Logs leaking sensitive data | Information Disclosure | Serilog `Override` excludes EF Core at Information+; no request body logging configured | V7 |
| T-1-04 | Default DB credentials committed | Information Disclosure | `docker-compose.yml` uses dev-only credentials; documented in SUMMARY that production uses env vars; `.gitignore` excludes `appsettings.Development.json` | V6 |
