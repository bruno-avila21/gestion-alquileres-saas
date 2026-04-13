# Phase 2: Gestión de Índices BCRA/INDEC — Research

**Researched:** 2026-04-13
**Domain:** External API consumption (.NET 8 HttpClient + Resilience), EF Core global entities, CQRS index sync
**Confidence:** HIGH (BCRA/INDEC API: MEDIUM — official API only partially testable from this environment)

---

## Summary

Phase 2 builds the ICL/IPC synchronization and persistence layer. The foundational Clean Architecture scaffolded in Phase 1 (MediatR, FluentValidation, EF Core with Npgsql snake_case, AppDbContext pattern) is fully in place and ready to extend. IndexValue is a **global entity** — no OrganizationId, no multi-tenant filter — which is explicitly supported by the existing AppDbContext pattern and called out in AGENTS.md.

The BCRA provides a public, **authentication-free** REST API (`api.bcra.gob.ar`) with ICL at variable ID 7988. The endpoint pattern for v1 (confirmed via community code analysis) is `/estadisticas/v1/datosvariable/{idVariable}/{desde}/{hasta}`, returning JSON with `{ fecha: "DD/MM/YYYY", valor: number }` fields. The v4.0 API is documented but the direct URL format for fetching individual variable data was not confirmable from this environment (400 responses). The v1 pattern is verified via multiple community implementations and should be treated as the safe choice; v4.0 endpoints should be verified during implementation.

For IPC, Argentina's `apis.datos.gob.ar` (Series de Tiempo API) provides national IPC data via series ID `148.3_INIVELNAL_DICI_M_26` — no authentication required, 40,000 requests/hour limit, clean JSON format `[[date, value], ...]`. This is a INDEC-sourced official government open data API.

Resilience is handled by `Microsoft.Extensions.Http.Resilience` 8.10.0 (not the deprecated `Microsoft.Extensions.Http.Polly`), which wraps Polly v8 via `AddStandardResilienceHandler()`. The project already has Hangfire configured in Infrastructure; Phase 2 uses **manual sync only** (POST /api/v1/indexes/sync triggered by admin), leaving background scheduling to Phase 5 where it becomes critical for CALC-04.

**Primary recommendation:** Implement BcraApiClient against the v1 endpoint (verified pattern), add resilience via `Microsoft.Extensions.Http.Resilience` 8.10.0, design IndexValue with a composite unique constraint on `(IndexType, Period)` to prevent duplicate sync, and use `DateOnly` for Period storage.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

No CONTEXT.md exists for Phase 2 — constraints are sourced from CLAUDE.md and planning documents.

### Locked Decisions (from CLAUDE.md + AGENTS.md + STATE.md)
- Clean Architecture: all four layers strictly maintained
- IndexValue entity: NO OrganizationId, NO global query filter (explicitly in AGENTS.md)
- Índices persistidos: NEVER calculate ICL/IPC from live API calls — always persist first, then calculate
- If BCRA API fails: log warning + use last available value from DB
- EF Core: snake_case naming convention (UseSnakeCaseNamingConvention already in DI)
- EF Core: NUNCA SQL raw, always LINQ + repository interfaces
- All controllers must have [Authorize] on routes (admin-only sync)
- OrganizationId: NEVER from request body (not applicable here — indexes are global)
- NEVER IgnoreQueryFilters() in production

### Claude's Discretion
- Whether to use BCRA API v1 vs v4.0 (verify during implementation)
- Whether IndecApiClient calls datos.gob.ar directly or uses an alternative
- Exact Period type: `DateOnly` vs `string "YYYY-MM"` (DateOnly preferred)
- Background sync scheduling: Phase 2 is manual-only; Hangfire job deferred to Phase 5
- Frontend table pagination (server-side vs client-side for ~50 records/year)

### Deferred Ideas (OUT OF SCOPE for Phase 2)
- Automatic scheduled sync via Hangfire (Phase 5 — AjusteMensualJob)
- ICL/IPC calculation logic (Phase 5)
- RentHistory and Transaction creation (Phase 5)
- Contract management (Phase 4)
- Property/Tenant CRUD (Phase 3)
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| IDX-01 | Servicio consume API BCRA para obtener valores ICL por período | BcraApiClient using IHttpClientFactory; BCRA endpoint `/estadisticas/v1/datosvariable/7988/{desde}/{hasta}` |
| IDX-02 | Servicio consume API INDEC para obtener valores IPC por período | IndecApiClient using datos.gob.ar Series API; series ID `148.3_INIVELNAL_DICI_M_26` |
| IDX-03 | Valores persistidos en tabla `Indexes` antes de usar en cálculos | IndexValue entity + IIndexRepository; EF Core config without global query filter |
| IDX-04 | Fallback a último valor disponible si API falla; log warning | `GetLastAvailableAsync` on IIndexRepository; Serilog _logger.LogWarning; try/catch in SyncIndexCommandHandler |
| IDX-05 | Endpoint para sincronización manual de índices | POST /api/v1/indexes/sync — [Authorize] admin route → SyncIndexCommand |
| IDX-06 | Endpoint para consultar índices históricos por tipo y rango de fechas | GET /api/v1/indexes?type={}&from={}&to={} → GetIndexByPeriodQuery |
</phase_requirements>

---

## Standard Stack

### Core (new additions for Phase 2)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.Extensions.Http.Resilience | 8.10.0 | HttpClient retry + circuit breaker + timeout | Replaces deprecated Microsoft.Extensions.Http.Polly; built on Polly v8; AddStandardResilienceHandler() is idiomatic for .NET 8 |

**Verified version:** `Microsoft.Extensions.Http.Resilience 8.10.0` — confirmed compatible with `net8.0` target framework, successfully installs without conflicts. [VERIFIED: NuGet install test in project]

### Already Available (Phase 1 — no new installs needed)

| Library | Version | Purpose |
|---------|---------|---------|
| MediatR | 12.4.1 | SyncIndexCommand + GetIndexByPeriodQuery handlers |
| FluentValidation | 11.11.0 | SyncIndexCommandValidator |
| EF Core 8 + Npgsql | 8.0.11 | IndexValue persistence, snake_case naming |
| AutoMapper | 16.1.1 | IndexValue → IndexValueDto |
| Serilog | (via AspNetCore 8.0.3) | Structured logging for fallback warnings |
| Hangfire | 1.8.23 | Already configured — NOT used for Phase 2 scheduling |
| BCrypt.Net-Next | 4.0.3 | Not relevant to Phase 2 |

**Installation (new package only):**
```bash
cd api
dotnet add src/GestionAlquileres.Infrastructure/GestionAlquileres.Infrastructure.csproj package Microsoft.Extensions.Http.Resilience --version 8.10.0
```

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Microsoft.Extensions.Http.Resilience | Microsoft.Extensions.Http.Polly | Polly is DEPRECATED for .NET 8; use Resilience package instead [VERIFIED: MS Learn docs 2026-02-24] |
| datos.gob.ar Series API for IPC | Direct INDEC website scraping | datos.gob.ar is the official open data API, no scraping needed |
| DateOnly for Period | DateTime | DateOnly is more semantically correct for monthly periods (no time component) — available in .NET 6+ |

---

## Architecture Patterns

### Recommended Project Structure (Phase 2 additions)

```
src/
  GestionAlquileres.Domain/
    Entities/
      IndexValue.cs              # NEW — no OrganizationId
    Enums/
      IndexType.cs               # NEW — ICL, IPC
    Interfaces/
      Repositories/
        IIndexRepository.cs      # NEW
      Services/
        IBcraApiClient.cs        # NEW (optional — can be concrete)
        IIndecApiClient.cs       # NEW (optional — can be concrete)

  GestionAlquileres.Application/
    Features/
      Indexes/
        Commands/
          SyncIndexCommand.cs                 # NEW
          SyncIndexCommandHandler.cs          # NEW
          SyncIndexCommandValidator.cs        # NEW
        Queries/
          GetIndexByPeriodQuery.cs            # NEW
          GetIndexByPeriodQueryHandler.cs     # NEW
        DTOs/
          IndexValueDto.cs                    # NEW

  GestionAlquileres.Infrastructure/
    ExternalServices/
      BcraApiClient.cs            # NEW — consumes BCRA API
      IndecApiClient.cs           # NEW — consumes datos.gob.ar
      BcraApiResponse.cs          # NEW — deserialization model
      IndecApiResponse.cs         # NEW — deserialization model
    Persistence/
      Configurations/
        IndexValueConfiguration.cs  # NEW — no global filter
      Repositories/
        IndexRepository.cs          # NEW

  GestionAlquileres.API/
    Controllers/
      IndexesController.cs        # NEW

web/src/
  features/
    indexes/
      IndexesPage.tsx             # NEW — admin page
      indexService.ts             # NEW — axios calls
      useIndexes.ts               # NEW — TanStack Query hook
```

### Pattern 1: Global Entity (No Tenant Filter)

**What:** IndexValue does NOT have OrganizationId. The EF configuration deliberately omits `HasQueryFilter`. This follows the pattern already established in AppDbContext for Organization itself.

**When to use:** Any data shared across all tenants (indices, reference tables, system config).

```csharp
// Source: api/AGENTS.md — AppDbContext pattern comment
// Infrastructure/Persistence/Configurations/IndexValueConfiguration.cs
public class IndexValueConfiguration : IEntityTypeConfiguration<IndexValue>
{
    public void Configure(EntityTypeBuilder<IndexValue> builder)
    {
        builder.ToTable("index_values");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.IndexType).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Period).IsRequired();      // DateOnly maps to 'date' in PostgreSQL
        builder.Property(x => x.Value).HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.VariationPct).HasPrecision(10, 6);
        builder.Property(x => x.Source).HasMaxLength(100).IsRequired();
        builder.Property(x => x.FetchedAt)
               .HasDefaultValueSql("now()");

        // Composite unique constraint — prevents duplicate sync for same period
        builder.HasIndex(x => new { x.IndexType, x.Period }).IsUnique();
    }
}
// NOTE: AppDbContext does NOT add HasQueryFilter for IndexValue
// [CITED: api/AGENTS.md — "IndexValue NO tiene filtro multi-tenant — los índices son globales (BCRA)"]
```

### Pattern 2: BcraApiClient — Typed HttpClient with Resilience

**What:** Infrastructure service that calls BCRA API and returns deserialized data. Registered as typed HttpClient with standard resilience handler.

**When to use:** All external API calls in Infrastructure layer.

```csharp
// Source: BCRA API v1 endpoint — verified via community implementations
// [VERIFIED: github.com/JuanCassinerio/api-BCRA — v1 path confirmed]
// Infrastructure/ExternalServices/BcraApiClient.cs

public class BcraApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BcraApiClient> _logger;
    private const int IclVariableId = 7988;

    public BcraApiClient(HttpClient httpClient, ILogger<BcraApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BcraDataPoint>> GetIclAsync(
        DateOnly desde, DateOnly hasta, CancellationToken ct = default)
    {
        // v1 path: /estadisticas/v1/datosvariable/{idVariable}/{YYYY-MM-DD}/{YYYY-MM-DD}
        // Response: { "status": 200, "results": [{ "fecha": "DD/MM/YYYY", "valor": 1.234 }] }
        var url = $"/estadisticas/v1/datosvariable/{IclVariableId}" +
                  $"/{desde:yyyy-MM-dd}/{hasta:yyyy-MM-dd}";

        var response = await _httpClient.GetFromJsonAsync<BcraResponse>(url, ct);
        return response?.Results ?? Array.Empty<BcraDataPoint>();
    }
}

// Deserialization models
public record BcraResponse(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("results")] List<BcraDataPoint>? Results
);

public record BcraDataPoint(
    [property: JsonPropertyName("fecha")] string Fecha,   // "DD/MM/YYYY"
    [property: JsonPropertyName("valor")] decimal Valor
);
```

**Registration in DependencyInjection.cs:**
```csharp
// Source: [CITED: learn.microsoft.com/dotnet/core/resilience/http-resilience]
services.AddHttpClient<BcraApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.bcra.gob.ar");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromSeconds(2);
    // POST sync endpoint should not be retried — but GET is safe
});
```

### Pattern 3: IndecApiClient — datos.gob.ar Series API

**What:** Consumes Argentina's official open data time series API for IPC data.

**Endpoint:** `GET https://apis.datos.gob.ar/series/api/series/?ids=148.3_INIVELNAL_DICI_M_26&start_date={from}&end_date={to}&limit=50`

**Response format:** `{ "data": [["YYYY-MM-DD", 100.0], ["YYYY-MM-DD", 101.5]], "count": 111, "meta": [...] }`

**Notes:**
- No authentication required [VERIFIED: live API test]
- Rate limit: 40,000 requests/hour [CITED: datosgobar.github.io/series-tiempo-ar-api/terms/]
- Date is first of month for monthly data (e.g., "2024-01-01" = January 2024)
- Values are IPC Index points (base Dec 2016 = 100), not percentage variation

```csharp
// Infrastructure/ExternalServices/IndecApiClient.cs
public class IndecApiClient
{
    private readonly HttpClient _httpClient;
    private const string IpcSeriesId = "148.3_INIVELNAL_DICI_M_26";

    public IndecApiClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<IndecDataPoint>> GetIpcAsync(
        DateOnly desde, DateOnly hasta, CancellationToken ct = default)
    {
        var url = $"/series/api/series/?ids={IpcSeriesId}" +
                  $"&start_date={desde:yyyy-MM-dd}&end_date={hasta:yyyy-MM-dd}&limit=50";

        var response = await _httpClient.GetFromJsonAsync<IndecResponse>(url, ct);
        return response?.Data?.Select(row => new IndecDataPoint(
            DateOnly.FromDateTime(DateTime.Parse(row[0].GetString()!)),
            row[1].GetDecimal()
        )).ToList() ?? new List<IndecDataPoint>();
    }
}
// Note: IndecResponse.Data is List<JsonElement[]> since API returns array-of-arrays
```

### Pattern 4: SyncIndexCommandHandler with Fallback

**What:** Attempts to fetch from external API; if it fails, retrieves last available value from DB and logs a warning.

```csharp
// Application/Features/Indexes/Commands/SyncIndexCommandHandler.cs
public class SyncIndexCommandHandler : IRequestHandler<SyncIndexCommand, SyncIndexResult>
{
    private readonly BcraApiClient _bcraClient;
    private readonly IndecApiClient _indecClient;
    private readonly IIndexRepository _indexRepo;
    private readonly ILogger<SyncIndexCommandHandler> _logger;

    public async Task<SyncIndexResult> Handle(SyncIndexCommand request, CancellationToken ct)
    {
        var period = request.Period; // DateOnly

        IndexValue? existing = await _indexRepo.GetByPeriodAsync(request.IndexType, period, ct);
        if (existing is not null)
            return SyncIndexResult.AlreadySynced(existing);

        try
        {
            IndexValue fetched = request.IndexType == IndexType.ICL
                ? await FetchIclAsync(period, ct)
                : await FetchIpcAsync(period, ct);

            await _indexRepo.AddAsync(fetched, ct);
            return SyncIndexResult.Success(fetched);
        }
        catch (Exception ex)
        {
            // IDX-04: Fallback to last available value
            _logger.LogWarning(ex,
                "External API unavailable for {IndexType} period {Period}. Using last available value.",
                request.IndexType, period);

            var fallback = await _indexRepo.GetLastAvailableAsync(request.IndexType, ct);
            if (fallback is null)
                throw new BusinessException(
                    $"No se pudo obtener {request.IndexType} para {period:yyyy-MM} y no hay valor previo disponible.");

            return SyncIndexResult.Fallback(fallback);
        }
    }
}
```

### Pattern 5: IIndexRepository Interface

```csharp
// Domain/Interfaces/Repositories/IIndexRepository.cs
public interface IIndexRepository
{
    Task<IndexValue?> GetByPeriodAsync(IndexType type, DateOnly period, CancellationToken ct);
    Task<IndexValue?> GetLastAvailableAsync(IndexType type, CancellationToken ct);
    Task<IReadOnlyList<IndexValue>> GetRangeAsync(IndexType type, DateOnly from, DateOnly to, CancellationToken ct);
    Task AddAsync(IndexValue indexValue, CancellationToken ct);
    Task<bool> ExistsAsync(IndexType type, DateOnly period, CancellationToken ct);
}
```

### Anti-Patterns to Avoid

- **Calculating ICL/IPC from live API calls:** NEVER pass the freshly-fetched value directly into a rent calculation. Always persist to `index_values` table first, then query from DB. [CRITICAL — from CLAUDE.md]
- **Adding OrganizationId to IndexValue:** These are shared global values. Adding a tenant discriminator would require syncing the same data per organization — wasteful and incorrect.
- **Returning DateTime for Period:** Use `DateOnly` — there is no time component to a monthly index period.
- **Not validating idempotency:** SyncIndexCommand should be a no-op if the period is already persisted. Check before inserting to avoid unique constraint violations.
- **Retrying POST operations:** The sync endpoint uses POST but is idempotent by design (check-then-insert). However, the resilience handler's default retries on POST should be configured with `DisableForUnsafeHttpMethods()` for the BCRA client to avoid double-insert race conditions.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| HTTP retry + circuit breaker | Custom retry loops / sleep-and-retry | `Microsoft.Extensions.Http.Resilience` with `AddStandardResilienceHandler()` | Handles jitter, exponential backoff, circuit breaker, timeout out of the box |
| JSON deserialization | Manual string parsing of API responses | `System.Text.Json` via `GetFromJsonAsync<T>` | Already in .NET 8 BCL — no extra package |
| HttpClient lifecycle | `new HttpClient()` per request | `IHttpClientFactory` via typed clients | Socket exhaustion, DNS refresh — critical problem with naive HttpClient usage |
| Date-only period representation | Storing periods as `"2024-01"` strings | `DateOnly` type (maps to PostgreSQL `date`) | Type-safe, sortable, supports range queries naturally |

**Key insight:** Argentine government APIs (BCRA, datos.gob.ar) are free and require no API keys. The complexity is entirely in resilience (APIs go down regularly) and data normalization (different date formats per API).

---

## Common Pitfalls

### Pitfall 1: BCRA API Date Format Mismatch

**What goes wrong:** BCRA v1 API returns dates as `"DD/MM/YYYY"` (e.g., `"13/04/2024"`), not ISO 8601. Deserializing directly into a `DateOnly` or `DateTime` will fail or parse incorrectly.

**Why it happens:** BCRA uses Argentine date convention, not ISO standard.

**How to avoid:** Deserialize `fecha` as a `string` property, then parse manually: `DateOnly.ParseExact(dataPoint.Fecha, "dd/MM/yyyy", CultureInfo.InvariantCulture)`.

**Warning signs:** Getting `01/04/2024` interpreted as January 4th instead of April 1st.

### Pitfall 2: Period Granularity Mismatch (Daily vs Monthly)

**What goes wrong:** BCRA ICL is published daily. Storing every daily value bloats the table and complicates period-based queries (which Phase 5 needs monthly).

**Why it happens:** The BCRA API returns one record per business day, not one per month.

**How to avoid:** When syncing ICL, fetch the last available value within the target month (e.g., last business day of the month) and store `Period = new DateOnly(year, month, 1)`. The `SyncIndexCommand` should normalize daily → monthly.

**Warning signs:** Phase 5 calculator can't find `GetByPeriodAsync(ICL, 2024-03-01)` because the table has `2024-03-28`, `2024-03-27`, etc.

### Pitfall 3: Unique Constraint Violation on Concurrent Sync

**What goes wrong:** Two admin users trigger sync simultaneously for the same period → both check `ExistsAsync` → both find false → both insert → PostgreSQL unique constraint violation on `(IndexType, Period)`.

**Why it happens:** No distributed lock around the check-then-insert pattern.

**How to avoid:** Use PostgreSQL `INSERT ... ON CONFLICT DO NOTHING` via EF Core's `ExecuteRawSql` pattern (one acceptable exception to the no-raw-SQL rule), OR catch the unique constraint exception and treat it as success (the other request already inserted the value).

**Warning signs:** `PostgresException: 23505 duplicate key value violates unique constraint`.

### Pitfall 4: BCRA API Intermittent Failures

**What goes wrong:** BCRA API goes down (common on weekends, Argentine holidays, or during Banco Central maintenance windows). Sync fails hard, no useful error message.

**Why it happens:** Argentine government APIs have historically poor uptime SLAs.

**How to avoid:** `AddStandardResilienceHandler` handles transient failures. For complete outages: implement `GetLastAvailableAsync` fallback (IDX-04). Log with `LogWarning` (not `LogError`) since fallback succeeds.

**Warning signs:** `HttpRequestException` or `BrokenCircuitException` without a try/catch in the handler.

### Pitfall 5: IndecApiClient Array-of-Arrays Deserialization

**What goes wrong:** The datos.gob.ar API returns `data` as `[[date_string, value], ...]` not `[{date: ..., value: ...}]`. Binding to a typed DTO with `[JsonPropertyName]` won't work.

**Why it happens:** Series de Tiempo API uses a compact array format to minimize payload size.

**How to avoid:** Deserialize `Data` as `List<JsonElement[]>` or `List<List<JsonElement>>`, then project: `row[0].GetString()` for date, `row[1].GetDecimal()` for value.

---

## Code Examples

### IndexValue Entity

```csharp
// Source: [ASSUMED] — follows Domain entity pattern from api/AGENTS.md
// Domain/Entities/IndexValue.cs
public class IndexValue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public IndexType IndexType { get; set; }
    public DateOnly Period { get; set; }      // First day of month: 2024-03-01 = March 2024
    public decimal Value { get; set; }
    public decimal? VariationPct { get; set; }
    public string Source { get; set; } = string.Empty;  // "BCRA" or "INDEC"
    public DateTimeOffset FetchedAt { get; set; } = DateTimeOffset.UtcNow;
}
// NOTE: No OrganizationId — indexes are global, not per-tenant
```

### IndexType Enum

```csharp
// Domain/Enums/IndexType.cs
public enum IndexType
{
    ICL = 1,   // Índice para Contratos de Locación (BCRA variable 7988)
    IPC = 2    // Índice de Precios al Consumidor (datos.gob.ar / INDEC)
}
```

### IndexesController

```csharp
// Source: follows Controller pattern from api/AGENTS.md
// API/Controllers/IndexesController.cs
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]  // All endpoints require JWT
public class IndexesController : BaseController
{
    // GET /api/v1/indexes?type=ICL&from=2024-01-01&to=2024-12-31
    [HttpGet]
    public async Task<IActionResult> GetIndexes(
        [FromQuery] IndexType type,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetIndexByPeriodQuery(type, from, to), ct);
        return Ok(result);
    }

    // POST /api/v1/indexes/sync
    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromBody] SyncIndexRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SyncIndexCommand(request.IndexType, request.Period), ct);
        return Ok(result);
    }
}
```

### SyncIndexCommand (Application)

```csharp
// Application/Features/Indexes/Commands/SyncIndexCommand.cs
public record SyncIndexCommand(
    IndexType IndexType,
    DateOnly Period
) : IRequest<SyncIndexResult>;

public record SyncIndexResult(
    bool Success,
    bool WasFallback,
    bool AlreadyExisted,
    IndexValueDto IndexValue
)
{
    public static SyncIndexResult Success(IndexValue v) => new(true, false, false, Map(v));
    public static SyncIndexResult Fallback(IndexValue v) => new(true, true, false, Map(v));
    public static SyncIndexResult AlreadySynced(IndexValue v) => new(true, false, true, Map(v));
    // Map = AutoMapper in handler
}
```

### Frontend: useIndexes Hook Pattern

```typescript
// Source: [ASSUMED] — follows React 19 + TanStack Query pattern from Phase 1
// web/src/features/indexes/useIndexes.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { indexService } from './indexService'

export function useIndexes(type: 'ICL' | 'IPC', from: string, to: string) {
  return useQuery({
    queryKey: ['indexes', type, from, to],
    queryFn: () => indexService.getIndexes(type, from, to),
  })
}

export function useSyncIndex() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: indexService.syncIndex,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['indexes'] })
    },
  })
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `Microsoft.Extensions.Http.Polly` | `Microsoft.Extensions.Http.Resilience` | .NET 8 release | Polly package deprecated; Resilience package is the standard [VERIFIED: MS docs] |
| Custom retry loop with `Thread.Sleep` | `AddStandardResilienceHandler()` | .NET 8+ | 5-strategy pipeline (rate limiter, total timeout, retry, circuit breaker, attempt timeout) out of the box |
| BCRA unofficial API (estadisticasbcra.com) | Official BCRA API (api.bcra.gob.ar) | ~2024 | Official API is free, no token required, no 100-request/day limit |

**Deprecated/outdated:**
- `estadisticasbcra.com` unofficial API: requires token, 100 requests/day limit, "no guarantee of future availability" — do NOT use.
- `Microsoft.Extensions.Http.Polly`: explicitly deprecated, use `Microsoft.Extensions.Http.Resilience` instead.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | All compilation | Yes | 9.0.304 (targets net8.0) | — |
| PostgreSQL | EF Core migrations | Via Docker | 29.2.1 (Docker) | Start Docker container |
| BCRA API (api.bcra.gob.ar) | BcraApiClient | MEDIUM | Unknown — 400 on direct test | Test manually during implementation |
| datos.gob.ar API | IndecApiClient | Yes | Active | — |
| Microsoft.Extensions.Http.Resilience 8.10.0 | BcraApiClient/IndecApiClient | Yes (installable) | 8.10.0 | — |

**BCRA API note:** Direct calls to `api.bcra.gob.ar` returned HTTP 400 from this research environment (possibly due to Windows proxy/networking). The endpoint pattern `v1/datosvariable/{id}/{desde}/{hasta}` is confirmed via multiple independent community implementations. The v4.0 path `estadisticas/v4.0/Monetarias/{id}` is documented but the exact query parameter format for date ranges is not confirmed. Recommendation: implement against v1 first, then test manually in the dev environment.

**Missing dependencies with no fallback:** None.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (existing in Phase 1 test project) |
| Config file | `api/tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj` |
| Quick run command | `cd api && dotnet test --filter "Category=Unit"` |
| Full suite command | `cd api && dotnet test` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| IDX-01 | BcraApiClient maps BCRA response to IndexValue correctly | Unit (mock HttpClient) | `dotnet test --filter "BcraApiClientTests"` | Wave 0 |
| IDX-02 | IndecApiClient maps datos.gob.ar response to IndexValue correctly | Unit (mock HttpClient) | `dotnet test --filter "IndecApiClientTests"` | Wave 0 |
| IDX-03 | SyncIndexCommandHandler persists IndexValue via repository | Unit (mock repo) | `dotnet test --filter "SyncIndexCommandHandlerTests"` | Wave 0 |
| IDX-04 | Handler uses fallback when API throws; logs warning | Unit (mock client throws) | `dotnet test --filter "SyncIndex_FallbackTests"` | Wave 0 |
| IDX-05 | POST /api/v1/indexes/sync returns 200 and correct DTO | Integration | `dotnet test --filter "IndexesControllerTests"` | Wave 0 |
| IDX-06 | GET /api/v1/indexes returns filtered list by type and date range | Integration | `dotnet test --filter "IndexesControllerTests"` | Wave 0 |

### Sampling Rate
- **Per task commit:** `cd api && dotnet test --filter "Category=Unit" --no-build`
- **Per wave merge:** `cd api && dotnet test`
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps
- [ ] `api/tests/GestionAlquileres.Tests/Features/Indexes/SyncIndexCommandHandlerTests.cs`
- [ ] `api/tests/GestionAlquileres.Tests/Infrastructure/BcraApiClientTests.cs`
- [ ] `api/tests/GestionAlquileres.Tests/Infrastructure/IndecApiClientTests.cs`
- [ ] `api/tests/GestionAlquileres.Tests/Controllers/IndexesControllerTests.cs`

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | No | JWT auth inherited from Phase 1 BaseController |
| V3 Session Management | No | Stateless JWT — no sessions |
| V4 Access Control | Yes | [Authorize] on all IndexesController routes; sync is admin-only |
| V5 Input Validation | Yes | FluentValidation on SyncIndexCommand (IndexType enum, Period range) |
| V6 Cryptography | No | No encryption needed — public BCRA/INDEC data |

### Known Threat Patterns for this stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Unauthorized sync trigger (unauthenticated POST) | Elevation of Privilege | [Authorize] on IndexesController — JWT required |
| SSRF via configurable BCRA URL | Tampering | Base URL hardcoded in DI registration, NOT taken from request |
| Date range injection in query string | Tampering | `DateOnly` binding + FluentValidation range check (Period not in future, range <= 12 months) |
| Mass sync flooding (DoS via repeated POST /sync) | DoS | AddStandardResilienceHandler rate limiter; per-user rate limiting via ASP.NET Core rate limiting if needed |

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | BCRA API v1 endpoint `/estadisticas/v1/datosvariable/{id}/{desde}/{hasta}` is still active | Standard Stack, Code Examples | BcraApiClient URL won't work — need to switch to v4.0 path |
| A2 | BCRA ICL variable ID is 7988 | Code Examples | Fetched data will be wrong index — verify with `/estadisticas/v1/principalesvariables` |
| A3 | BCRA v1 response JSON has `fecha` (DD/MM/YYYY) and `valor` fields | Code Examples | Deserialization will fail — inspect live response and adjust |
| A4 | Phase 1 includes an xUnit test project | Validation Architecture | Need to create test project if missing |
| A5 | DateOnly maps correctly to PostgreSQL `date` type via Npgsql 8.x | Architecture Patterns | May need explicit `HasColumnType("date")` in EF configuration |

---

## Open Questions

1. **BCRA API v1 vs v4.0 path format**
   - What we know: v1 uses `/estadisticas/v1/datosvariable/{id}/{desde}/{hasta}` (confirmed via community code). v4.0 uses `/estadisticas/v4.0/Monetarias/{id}` with query params, but the exact param names for date range are unclear.
   - What's unclear: Whether v1 endpoint remains active in 2026 or has been deprecated.
   - Recommendation: Implement BcraApiClient targeting v1 first. On first real test run, verify the response. If v1 is deprecated, switch to v4.0 — which requires discovering the `desde`/`hasta` query parameter names from the official v4.0 PDF manual.

2. **ICL variable ID confirmation**
   - What we know: Variable 7988 appears in the BCRA "Principales Variables" page URL as `serie=7988&detalle=ICL`. Multiple search results consistently reference this ID.
   - What's unclear: Whether this ID is the same in v4.0 or has been renumbered.
   - Recommendation: On first successful API call, call `/estadisticas/v1/principalesvariables` and grep the response for "ICL" to confirm the variable ID. [ASSUMED — A2]

3. **xUnit test project location**
   - What we know: Phase 1 VERIFICATION.md confirmed "8/8 integration tests passing" — a test project exists.
   - What's unclear: Exact path and project name.
   - Recommendation: `ls api/tests/` to confirm before writing Wave 0 test files.

---

## Sources

### Primary (HIGH confidence)
- `api/AGENTS.md` — IndexValue entity patterns, AppDbContext global filter exclusion for indexes, command/handler/controller patterns, naming conventions
- `api/src/GestionAlquileres.Infrastructure/DependencyInjection.cs` — confirmed HttpClient registration pattern and project structure
- [learn.microsoft.com/dotnet/core/resilience/http-resilience](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience) — AddStandardResilienceHandler API, default strategies, retry configuration (dated 2026-02-24)
- [nuget.org/packages/Microsoft.Extensions.Http.Resilience/8.10.0](https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience/8.10.0) — net8.0 compatibility confirmed
- [apis.datos.gob.ar/series/api/series/?ids=148.3_INIVELNAL_DICI_M_26](https://apis.datos.gob.ar/series/api/series/?ids=148.3_INIVELNAL_DICI_M_26&limit=3&format=json) — IPC endpoint live response confirmed

### Secondary (MEDIUM confidence)
- [github.com/JuanCassinerio/api-BCRA](https://github.com/JuanCassinerio/api-BCRA) — BCRA v1 endpoint path `/estadisticas/v1/datosvariable/{id}/{desde}/{hasta}` and response field names `fecha`, `valor`
- [principales-variables.bcra.apidocs.ar](https://principales-variables.bcra.apidocs.ar/) — v4.0 endpoint `/estadisticas/v4.0/Monetarias/{IdVariable}` confirmed
- [datos.gob.ar/series/api](https://datos.gob.ar/series/api/series/?ids=148.3_INIVELNAL_DICI_M_26) — IPC series ID `148.3_INIVELNAL_DICI_M_26` confirmed as national IPC Nivel General Base dic 2016

### Tertiary (LOW confidence — verify during implementation)
- BCRA ICL variable ID 7988 — referenced in BCRA website URL parameter, consistent across multiple sources, but not verified via live API call
- BCRA API v1 endpoint still active in 2026 — assumed based on community code from 2024

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — packages verified via NuGet, MS docs verified via official source
- Architecture: HIGH — follows established Phase 1 patterns; IndexValue global entity pattern explicitly in AGENTS.md
- BCRA API endpoints: MEDIUM — v1 confirmed via community code; v4.0 path documented but date param format not confirmed
- INDEC/datos.gob.ar API: HIGH — live response verified
- Pitfalls: HIGH — date format, daily-vs-monthly granularity, and concurrent sync pitfalls are verifiable from known API behavior

**Research date:** 2026-04-13
**Valid until:** 2026-07-13 (90 days — APIs stable, MS packages stable; BCRA may restructure API)
