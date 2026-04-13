---
phase: 02-gestion-de-indices-bcra-indec
plan: "03"
subsystem: application-cqrs-index-sync
tags: [application, cqrs, mediatr, fluentvalidation, bcra, indec, fallback, tdd, phase2]
dependency_graph:
  requires:
    - 02-01 (IIndexRepository, IndexValue entity)
    - 02-02 (IBcraApiClient/IIndecApiClient via domain interfaces)
  provides:
    - SyncIndexCommand + SyncIndexCommandHandler (consumed by 02-04 API endpoint, 02-05 scheduler)
    - GetIndexByPeriodQuery + GetIndexByPeriodQueryHandler (consumed by 02-04 API endpoint)
    - IBcraApiClient / IIndecApiClient interfaces in Domain (available to any future handler)
    - BusinessException type in Application/Common/Exceptions
  affects:
    - BcraApiClient / IndecApiClient (implement domain interfaces, renamed point types)
    - Infrastructure DependencyInjection (typed clients now registered against interfaces)
    - Phase2 Infrastructure test files (added domain namespace import)
tech_stack:
  added: []
  patterns:
    - Interface-in-Domain for external API clients (avoids Application → Infrastructure reference)
    - Fetch-or-fallback pattern: ExistsAsync → external call → AddAsync+SaveChangesAsync (or GetLastAvailableAsync on failure)
    - Period normalization: any DateOnly input normalised to first-of-month before all operations
    - BusinessException : InvalidOperationException → maps to HTTP 409 via existing ExceptionMiddleware
    - Hand-rolled stubs (StubIndexRepoSync, StubBcraClient, StubIndecClient, CapturingLogger) — no Moq
key_files:
  created:
    - api/src/GestionAlquileres.Domain/Interfaces/Services/IBcraApiClient.cs
    - api/src/GestionAlquileres.Domain/Interfaces/Services/IIndecApiClient.cs
    - api/src/GestionAlquileres.Application/Common/Exceptions/BusinessException.cs
    - api/src/GestionAlquileres.Application/Features/Indexes/DTOs/IndexValueDto.cs
    - api/src/GestionAlquileres.Application/Features/Indexes/DTOs/SyncIndexResult.cs
    - api/src/GestionAlquileres.Application/Features/Indexes/Commands/SyncIndexCommand.cs
    - api/src/GestionAlquileres.Application/Features/Indexes/Commands/SyncIndexCommandValidator.cs
    - api/src/GestionAlquileres.Application/Features/Indexes/Commands/SyncIndexCommandHandler.cs
    - api/src/GestionAlquileres.Application/Features/Indexes/Queries/GetIndexByPeriodQuery.cs
    - api/src/GestionAlquileres.Application/Features/Indexes/Queries/GetIndexByPeriodQueryValidator.cs
    - api/src/GestionAlquileres.Application/Features/Indexes/Queries/GetIndexByPeriodQueryHandler.cs
    - api/tests/GestionAlquileres.Tests/Phase2/Application/SyncIndexCommandHandlerTests.cs
    - api/tests/GestionAlquileres.Tests/Phase2/Application/GetIndexByPeriodQueryHandlerTests.cs
  modified:
    - api/src/GestionAlquileres.Infrastructure/ExternalServices/BcraApiClient.cs (implements IBcraApiClient, uses BcraIndexPoint)
    - api/src/GestionAlquileres.Infrastructure/ExternalServices/IndecApiClient.cs (implements IIndecApiClient, uses IndecIndexPoint)
    - api/src/GestionAlquileres.Infrastructure/ExternalServices/BcraApiResponse.cs (removed BcraDataPoint public record)
    - api/src/GestionAlquileres.Infrastructure/ExternalServices/IndecApiResponse.cs (removed IndecDataPoint public record)
    - api/src/GestionAlquileres.Infrastructure/DependencyInjection.cs (AddHttpClient<IBcraApiClient, BcraApiClient> and IIndecApiClient)
    - api/tests/GestionAlquileres.Tests/Phase2/Infrastructure/BcraApiClientTests.cs (added domain namespace)
    - api/tests/GestionAlquileres.Tests/Phase2/Infrastructure/IndecApiClientTests.cs (added domain namespace)
decisions:
  - "Interface-in-Domain path taken (IBcraApiClient/IIndecApiClient in Domain/Interfaces/Services) because Infrastructure already references Application — confirmed circular reference would result from direct Application → Infrastructure reference"
  - "BcraDataPoint/IndecDataPoint removed from Infrastructure; replaced by BcraIndexPoint/IndecIndexPoint in Domain — single source of truth for point shape"
  - "BusinessException : InvalidOperationException to leverage existing ExceptionMiddleware 409 mapping without middleware changes"
  - "NotEqual(default) replaced with Must(t => (int)t != 0) to resolve FluentValidation ambiguous overload for enum types"
  - "StubIndecClient.ToThrow field generates CS0649 warning (never assigned in happy-path tests) — intentional design, suppressed at test level only"
metrics:
  duration: "~25 minutes"
  completed: "2026-04-13"
  tasks_completed: 5
  files_created: 13
  files_modified: 7
---

# Phase 2 Plan 03: Application CQRS Layer — SyncIndexCommand + GetIndexByPeriodQuery Summary

SyncIndexCommandHandler (fetch-or-fallback with IDX-04 compliance) and GetIndexByPeriodQueryHandler wired to domain interfaces IBcraApiClient/IIndecApiClient and IIndexRepository. Domain interfaces introduced to avoid Application→Infrastructure circular reference. All 36 Phase2 tests pass.

## Clean Architecture Path Taken

**Interface-in-Domain** — `IBcraApiClient` and `IIndecApiClient` created in `GestionAlquileres.Domain/Interfaces/Services/`. Concrete `BcraApiClient`/`IndecApiClient` implement these interfaces. Infrastructure DI registers `AddHttpClient<IBcraApiClient, BcraApiClient>`. Application handlers inject domain interfaces only.

Rationale: `GestionAlquileres.Infrastructure.csproj` already references `GestionAlquileres.Application` — adding the reverse reference would create a circular dependency.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 03-00 | Domain interfaces + BusinessException + update clients | de4576f | 10 files (3 created, 7 modified) |
| 03-01 | RED failing handler tests | 18b6adf | 2 test files created |
| 03-02 | DTOs + Commands + Queries + Validators | f600df6 | 6 files created |
| 03-03 | SyncIndexCommandHandler | c4ec6c8 | 1 file created |
| 03-04 | GetIndexByPeriodQueryHandler | c4ec6c8 | 1 file created (same commit as 03-03) |

## Test Results

- Phase2 total: **36/36 passed**, 0 failed, 0 skipped
  - Plan 01 Domain+Infrastructure: 11 tests
  - Plan 02 HTTP Clients: 9 tests
  - Plan 03 Application handlers: 16 tests
    - SyncIndexCommandHandlerTests: 8 (T1-T8)
    - SyncIndexCommandValidatorTests: 3
    - GetIndexByPeriodQueryHandlerTests: 3
    - GetIndexByPeriodQueryValidatorTests: 2

## SyncIndexCommandHandler Behavior (IDX-01 through IDX-04)

| Scenario | Behavior |
|----------|----------|
| Period already in DB | Returns `AlreadyExisted`, no external API call |
| External API succeeds | Fetches, normalizes to month-start, persists, returns `NewlySynced` |
| External API fails (any exception) | Logs `LogWarning`, calls `GetLastAvailableAsync`, returns `Fallback` |
| External API fails + no previous value | Throws `BusinessException("...no hay valor previo disponible")` |
| Empty API response | Treated as failure → fallback path |
| ICL source | "BCRA"; takes last-by-date daily point as monthly value |
| IPC source | "INDEC"; matches by year+month, falls back to latest if no exact match |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] FluentValidation ambiguous overload for enum NotEqual(default)**
- **Found during:** Task 03-02 (build verification)
- **Issue:** `RuleFor(x => x.IndexType).NotEqual(default)` is ambiguous in FluentValidation 11 when the property is an enum — CS0121 between two `NotEqual` overloads.
- **Fix:** Replaced with `.Must(t => (int)t != 0)` — equivalent semantics, no ambiguity.
- **Files modified:** SyncIndexCommandValidator.cs, GetIndexByPeriodQueryValidator.cs
- **Commit:** f600df6

**2. [Rule 3 - Blocking] file-scoped (file) stub types cannot appear in non-file method signatures**
- **Found during:** Task 03-01 build verification
- **Issue:** C# `file` access modifier on stub classes prevents their use as method parameter types in `internal`/`public` classes (CS9051).
- **Fix:** Changed all stub classes from `file sealed class` to `internal sealed class` with non-conflicting names (`StubIndexRepoSync`, `StubBcraClient`, `StubIndecClient`).
- **Files modified:** SyncIndexCommandHandlerTests.cs
- **Commit:** 18b6adf (rewrite of test file)

## Known Stubs

None. All handlers are fully implemented with real logic. No placeholder data or TODO paths.

## Threat Flags

| Flag | File | Description |
|------|------|-------------|
| threat_flag: input-validation | SyncIndexCommandValidator.cs | T-02-12 mitigated: Period > today rejected. Future periods blocked before handler runs. |
| threat_flag: input-validation | GetIndexByPeriodQueryValidator.cs | T-02-13 mitigated: From > To inverted range rejected. |
| threat_flag: repudiation | SyncIndexCommandHandler.cs | T-02-14 mitigated: LogWarning records exception + IndexType + Period on every fallback activation. |

## Self-Check: PASSED

Files verified:
- api/src/GestionAlquileres.Domain/Interfaces/Services/IBcraApiClient.cs: FOUND
- api/src/GestionAlquileres.Domain/Interfaces/Services/IIndecApiClient.cs: FOUND
- api/src/GestionAlquileres.Application/Common/Exceptions/BusinessException.cs: FOUND
- api/src/GestionAlquileres.Application/Features/Indexes/DTOs/IndexValueDto.cs: FOUND
- api/src/GestionAlquileres.Application/Features/Indexes/DTOs/SyncIndexResult.cs: FOUND
- api/src/GestionAlquileres.Application/Features/Indexes/Commands/SyncIndexCommand.cs: FOUND
- api/src/GestionAlquileres.Application/Features/Indexes/Commands/SyncIndexCommandValidator.cs: FOUND
- api/src/GestionAlquileres.Application/Features/Indexes/Commands/SyncIndexCommandHandler.cs: FOUND
- api/src/GestionAlquileres.Application/Features/Indexes/Queries/GetIndexByPeriodQuery.cs: FOUND
- api/src/GestionAlquileres.Application/Features/Indexes/Queries/GetIndexByPeriodQueryValidator.cs: FOUND
- api/src/GestionAlquileres.Application/Features/Indexes/Queries/GetIndexByPeriodQueryHandler.cs: FOUND

Commits verified: de4576f, 18b6adf, f600df6, c4ec6c8 — all present in git log.

Build: 0 errors, 2 warnings (CS0649 in test file — intentional stub field; xUnit2012 analyzer suggestion).
Tests: 36/36 Phase2 tests pass.
