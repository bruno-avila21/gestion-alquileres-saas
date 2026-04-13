---
phase: 02-gestion-de-indices-bcra-indec
plan: "02"
subsystem: infrastructure-http-clients
tags: [bcra, indec, http-client, resilience, tdd, phase2]
dependency_graph:
  requires: []
  provides: [BcraApiClient, IndecApiClient, typed-http-clients-with-resilience]
  affects: [02-03-sync-command-handler, 02-04-api-endpoint]
tech_stack:
  added:
    - Microsoft.Extensions.Http.Resilience 8.10.0
  patterns:
    - Typed HttpClient with AddStandardResilienceHandler
    - HttpMessageHandler stub for unit tests (no WireMock)
    - DateOnly.ParseExact with InvariantCulture for DD/MM/YYYY
    - EnsureSuccessStatusCode pattern for 5xx propagation
key_files:
  created:
    - api/src/GestionAlquileres.Infrastructure/ExternalServices/BcraApiResponse.cs
    - api/src/GestionAlquileres.Infrastructure/ExternalServices/BcraApiClient.cs
    - api/src/GestionAlquileres.Infrastructure/ExternalServices/IndecApiResponse.cs
    - api/src/GestionAlquileres.Infrastructure/ExternalServices/IndecApiClient.cs
    - api/tests/GestionAlquileres.Tests/Phase2/Infrastructure/BcraApiClientTests.cs
    - api/tests/GestionAlquileres.Tests/Phase2/Infrastructure/IndecApiClientTests.cs
  modified:
    - api/src/GestionAlquileres.Infrastructure/GestionAlquileres.Infrastructure.csproj
    - api/src/GestionAlquileres.Infrastructure/DependencyInjection.cs
decisions:
  - "Used GetAsync + EnsureSuccessStatusCode instead of GetFromJsonAsync to guarantee HttpRequestException propagates on 5xx (GetFromJsonAsync in .NET 8 does NOT throw on non-success by default)"
  - "HttpMessageHandler stub approach chosen over WireMock to keep test dependencies minimal — matches existing test patterns"
  - "Base URLs hardcoded in DI (not from configuration) to prevent SSRF (threat T-02-06)"
metrics:
  duration_seconds: 271
  completed_date: "2026-04-13T10:12:19Z"
  tasks_completed: 5
  files_created: 6
  files_modified: 2
---

# Phase 2 Plan 02: BCRA and INDEC HTTP Clients with Resilience Summary

Typed HttpClients for BCRA ICL and INDEC IPC APIs with AddStandardResilienceHandler resilience pipeline, DD/MM/YYYY date parsing, and array-of-arrays JSON deserialization — all 9 tests pass.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 02-01 | Install Microsoft.Extensions.Http.Resilience | 8b68035 | GestionAlquileres.Infrastructure.csproj |
| 02-02 | Write failing RED tests for BcraApiClient and IndecApiClient | 7b3cadf | BcraApiClientTests.cs, IndecApiClientTests.cs |
| 02-03 | Implement BcraApiClient + BcraApiResponse | 8daddf1 | BcraApiResponse.cs, BcraApiClient.cs |
| 02-04 | Implement IndecApiClient + IndecApiResponse | e10b115 | IndecApiResponse.cs, IndecApiClient.cs |
| 02-05 | Register typed HttpClients with AddStandardResilienceHandler | 0511343 | DependencyInjection.cs |

## Test Results

- BcraApiClientTests: 5/5 passing
- IndecApiClientTests: 4/4 passing
- Total: 9/9 passing

## Package Installed

`Microsoft.Extensions.Http.Resilience 8.10.0` added to `GestionAlquileres.Infrastructure.csproj`.

## Resilience Configuration Applied

Both `BcraApiClient` and `IndecApiClient` registered via `AddHttpClient<T>().AddStandardResilienceHandler(...)` with:
- `MaxRetryAttempts = 3`, exponential backoff with jitter (mitigates retry storm T-02-10)
- `AttemptTimeout = 10s`, `TotalRequestTimeout = 60s`
- Circuit breaker from default pipeline (mitigates cascade failure T-02-07)
- HTTPS base URLs hardcoded — not from config (prevents SSRF T-02-06)

## Public API Surface (consumed by Plan 03)

```csharp
// BcraApiClient
public Task<IReadOnlyList<BcraDataPoint>> GetIclAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default);
public record BcraDataPoint(DateOnly Fecha, decimal Valor);

// IndecApiClient
public Task<IReadOnlyList<IndecDataPoint>> GetIpcAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default);
public record IndecDataPoint(DateOnly Fecha, decimal Valor);
```

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] GetFromJsonAsync does not throw on 5xx in .NET 8**
- **Found during:** Task 02-03 (verifying test 5 for BcraApiClient)
- **Issue:** `GetFromJsonAsync` in .NET 8 does NOT call `EnsureSuccessStatusCode` — it returns null on 5xx instead of throwing `HttpRequestException`. The plan assumed `GetFromJsonAsync` would propagate 5xx as `HttpRequestException`.
- **Fix:** Changed implementation to `GetAsync` + `response.EnsureSuccessStatusCode()` + `response.Content.ReadFromJsonAsync<T>()` — this guarantees `HttpRequestException` on any non-success status code, enabling the IDX-04 fallback path in the SyncIndexCommandHandler.
- **Files modified:** BcraApiClient.cs, IndecApiClient.cs
- **Commits:** 8daddf1, e10b115

## Known Stubs

None — all client methods are fully implemented and wired to real HTTP endpoints.

## Threat Flags

No new security-relevant surface beyond what was already modeled in the plan's `<threat_model>`. All T-02-06 through T-02-11 mitigations applied as specified.

## Self-Check: PASSED

- [x] `api/src/GestionAlquileres.Infrastructure/ExternalServices/BcraApiClient.cs` — exists
- [x] `api/src/GestionAlquileres.Infrastructure/ExternalServices/IndecApiClient.cs` — exists
- [x] `api/src/GestionAlquileres.Infrastructure/ExternalServices/BcraApiResponse.cs` — exists
- [x] `api/src/GestionAlquileres.Infrastructure/ExternalServices/IndecApiResponse.cs` — exists
- [x] `api/tests/GestionAlquileres.Tests/Phase2/Infrastructure/BcraApiClientTests.cs` — exists
- [x] `api/tests/GestionAlquileres.Tests/Phase2/Infrastructure/IndecApiClientTests.cs` — exists
- [x] Commits 8b68035, 7b3cadf, 8daddf1, e10b115, 0511343 — all present in git log
- [x] `dotnet build` — 0 errors, 0 warnings
- [x] 9/9 tests pass
- [x] `grep -c "AddStandardResilienceHandler" DependencyInjection.cs` = 2
- [x] Package `Microsoft.Extensions.Http.Resilience 8.10.0` in Infrastructure.csproj
