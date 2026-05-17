---
phase: 02-gestion-de-indices-bcra-indec
plan: "04"
subsystem: api-http-surface-indexes
tags: [api, controller, integration-tests, jwt, webapplicationfactory, phase2]
dependency_graph:
  requires:
    - 02-03 (SyncIndexCommand, GetIndexByPeriodQuery, handlers, validators)
  provides:
    - GET /api/v1/indexes (IDX-06)
    - POST /api/v1/indexes/sync (IDX-05)
    - 10 integration tests covering auth, validation, sync, fallback, idempotency, and query
  affects:
    - Program.cs (JsonStringEnumConverter added to serialization pipeline)
tech_stack:
  added: []
  patterns:
    - WebApplicationFactory<Program> with IClassFixture for shared test host
    - PostConfigure<JwtBearerOptions> to guarantee token generation and validation use the same key in tests
    - PostConfigure<JwtSettings> to override IOptions<JwtSettings> after DI registration
    - JsonStringEnumConverter registered globally — API accepts/returns "ICL"/"IPC" strings
    - Stubs (StubBcra, StubIndec) registered as singletons via PostConfigure<JwtBearerOptions>
key_files:
  created:
    - api/src/GestionAlquileres.API/Controllers/IndexesController.cs
    - api/src/GestionAlquileres.API/Contracts/SyncIndexRequest.cs
    - api/tests/GestionAlquileres.Tests/Phase2/API/IndexesControllerTests.cs
  modified:
    - api/src/GestionAlquileres.API/Program.cs  (AddJsonOptions with JsonStringEnumConverter)
---

## What Was Built

`IndexesController` exposes the two Phase 2 HTTP endpoints:

- **GET /api/v1/indexes** — historical query (IDX-06). Delegates to `GetIndexByPeriodQuery`.
- **POST /api/v1/indexes/sync** — manual sync trigger (IDX-05). Delegates to `SyncIndexCommand`.

Both inherit `[Authorize]` from `BaseController`. `SyncIndexRequest` carries the POST body.
`JsonStringEnumConverter` was added to `AddControllers().AddJsonOptions()` in `Program.cs` so
the API accepts and returns enum values as strings (`"ICL"`, `"IPC"`) rather than integers.

## Test Results

| Suite | Passed | Total |
|-------|--------|-------|
| IndexesControllerTests (Plan 04) | 10 | 10 |
| Phase 2 total | 46 | 46 |

## Deviations From Plan

### 1. JWT Bearer options captured at startup — PostConfigure fix required

**Plan assumed:** `WebApplicationFactory.ConfigureAppConfiguration` overrides would be visible
when `Program.cs` reads `builder.Configuration` to set up `TokenValidationParameters`.

**Reality:** `Program.cs` captures `jwtSettings` from `builder.Configuration` **before**
`WebApplicationFactory.ConfigureAppConfiguration` sources are available. This means the JWT
Bearer validation parameters (Issuer, Audience, Key) use **production** values from
`appsettings.json`, while `IOptions<JwtSettings>` (lazy DI resolution at request time) sees
the test override values. Result: token is signed with test key, validated with production key →
401 for all authenticated tests.

**Fix:** Added `PostConfigure<JwtBearerOptions>` and `PostConfigure<JwtSettings>` in the test
factory's `ConfigureServices`. These run after all `Configure` calls and override both sides
(token generation via `JwtService` and token validation via JWT Bearer middleware) with the
same known test key/issuer/audience:

```csharp
const string testKey = "THIS_IS_A_TEST_SECRET_KEY_32_CHARS_MINIMUM";
services.PostConfigure<JwtSettings>(o =>
{
    o.Issuer = "TestIssuer";
    o.Audience = "TestAudience";
    o.SecretKey = testKey;
    o.ExpiryHours = 1;
});
services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
{
    o.TokenValidationParameters = new TokenValidationParameters { ... };
});
```

**Future consideration:** Refactor `Program.cs` to configure `AddJwtBearer` via
`AddOptions<JwtBearerOptions>().Configure<IOptions<JwtSettings>>()` instead of reading
`builder.Configuration` directly. That would make `ConfigureAppConfiguration` alone sufficient.

### 2. Test periods must be in the past

**Plan assumed:** Unique future years (2028–2031) could be used to avoid DB collisions.

**Reality:**
- `SyncIndexCommandValidator` rejects periods where `Period > DateOnly.FromDateTime(DateTime.UtcNow)`.
- `GetIndexByPeriodQueryValidator` rejects queries where `To > today + 1 day`.
- All sync tests with 2028–2031 dates → 400 BadRequest instead of expected 200/409.

**Fix:** Changed all test periods to past dates:

| Test | Original period | Fixed period |
|------|-----------------|--------------|
| T3 (ICL sync) | 2030-03 | 2024-01 |
| T4 (ICL idempotent) | 2030-04 | 2024-02 |
| T6 (no fallback) | 2029-06 | 2024-03 (IPC, see below) |
| T7 (fallback) | 2029-02/03 | 2024-04 seed / 2024-05 sync |
| T8 (GET range) | 2028-xx | 2023-xx |
| T10 (IPC sync) | 2031-03 | 2024-06 |

### 3. T6 switched from ICL to IPC to preserve "no fallback" invariant

**Plan assumed:** A dedicated ICL period (2029-06) would have no prior DB data.

**Reality:** With `IClassFixture` (shared in-memory DB across all tests), T3 and T4 persist ICL
data for 2024-01 and 2024-02. When T6 runs after T3/T4, `GetLastAvailableAsync(ICL)` finds
that data and returns a fallback → 200 instead of 409.

**Fix:** T6 uses **IPC** (no IPC data exists at that point in the sequence) and
`StubIndec.ToThrow` instead of `StubBcra.ToThrow`. This correctly exercises the "no fallback
available" path.

### 4. ReadFromJsonAsync requires JsonStringEnumConverter on the test client side

**Plan noted:** JsonStringEnumConverter was needed for the server. Plan did not explicitly note
that test deserialization also needs it.

**Fix:** Added a shared `JsonOpts` field in the test class:
```csharp
private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
{
    Converters = { new JsonStringEnumConverter() }
};
```
All `ReadFromJsonAsync<SyncIndexResult>` and `ReadFromJsonAsync<IReadOnlyList<IndexValueDto>>`
calls pass `JsonOpts`.

## Manual Verification — Task 04-03

**Status: PENDING (human checkpoint)**

This task requires the developer to start the API locally, register an org via Swagger, and
POST sync requests against the live BCRA and INDEC APIs. Steps are documented in the plan.

Automated tests cover all paths with stubs. Live API verification (IDX-05/IDX-06 against real
external endpoints) is blocked on: local PostgreSQL running + migration applied + network access
to `api.bcra.gob.ar` and `apis.datos.gob.ar`.

Known risk documented in plan: BCRA v1 endpoint may be deprecated. If Step 6 returns 404, the
`BcraApiClient` URL pattern needs to change to v4.0 (`/estadisticas/v4.0/Monetarias/{IdVariable}`).
