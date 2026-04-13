---
phase: 02-gestion-de-indices-bcra-indec
plan: "01"
subsystem: domain-persistence
tags: [domain, infrastructure, ef-core, migration, index-values, bcra, indec]
dependency_graph:
  requires: []
  provides:
    - IIndexRepository contract (consumed by Plans 02-02, 02-03, 02-04, 02-05)
    - IndexValue entity (global, no tenant filter)
    - index_values table via EF Core migration
  affects:
    - AppDbContext (DbSet<IndexValue> Indexes added)
    - DependencyInjection (IIndexRepository scoped registration)
tech_stack:
  added: []
  patterns:
    - Global entity without HasQueryFilter (IndexValue has no OrganizationId)
    - Composite unique index via EF Core HasIndex().IsUnique()
    - DateOnly as PostgreSQL date column
    - Enum stored as smallint via HasConversion<short>()
key_files:
  created:
    - api/src/GestionAlquileres.Domain/Enums/IndexType.cs
    - api/src/GestionAlquileres.Domain/Entities/IndexValue.cs
    - api/src/GestionAlquileres.Domain/Interfaces/Repositories/IIndexRepository.cs
    - api/src/GestionAlquileres.Infrastructure/Persistence/Configurations/IndexValueConfiguration.cs
    - api/src/GestionAlquileres.Infrastructure/Persistence/Repositories/IndexRepository.cs
    - api/src/GestionAlquileres.Infrastructure/Persistence/Migrations/20260413101011_AddIndexValues.cs
    - api/src/GestionAlquileres.Infrastructure/Persistence/Migrations/20260413101011_AddIndexValues.Designer.cs
    - api/tests/GestionAlquileres.Tests/Phase2/Domain/IndexValueTests.cs
    - api/tests/GestionAlquileres.Tests/Phase2/Infrastructure/IndexRepositoryTests.cs
  modified:
    - api/src/GestionAlquileres.Infrastructure/Persistence/AppDbContext.cs
    - api/src/GestionAlquileres.Infrastructure/DependencyInjection.cs
    - api/src/GestionAlquileres.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs
decisions:
  - IndexValue has no OrganizationId and no HasQueryFilter — global reference data per AGENTS.md and CLAUDE.md
  - Enum stored as smallint (not string) to save storage and match ICL=1, IPC=2 contract
  - Composite unique index named ix_index_values_type_period_unique prevents duplicate sync per RESEARCH Pitfall 3
  - IndexRepository uses plain LINQ (no IgnoreQueryFilters needed — no filter is registered for IndexValue)
metrics:
  duration: "~15 minutes"
  completed: "2026-04-13"
  tasks_completed: 5
  files_created: 9
  files_modified: 3
---

# Phase 2 Plan 01: IndexValue Domain + Persistence Foundation Summary

Global `IndexValue` entity (no OrganizationId) with EF Core configuration, composite unique constraint on (IndexType, Period), IIndexRepository contract with 6 methods, concrete IndexRepository, DI registration, and EF Core migration creating the `index_values` table. 11 tests all pass green.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 01-02 | IndexType enum + IndexValue entity + IIndexRepository | 495da39 | 3 files created in Domain |
| 01-01 | Phase2 Wave 0 tests | 628d9b5 | 2 test files created |
| 01-03 | IndexValueConfiguration + AppDbContext DbSet | e44e9a2 | 2 files (1 created, 1 modified) |
| 01-04 | IndexRepository + DI registration | 743ee4f | 2 files (1 created, 1 modified) |
| 01-05 | EF Core migration AddIndexValues | 2b76eb7 | 3 migration files |

## Test Results

- `dotnet test --filter "Phase2"`: **11/11 passed**, 0 failed, 0 skipped
- Domain tests (IndexValueTests): 5 tests — Id default, FetchedAt UTC, no OrganizationId, DateOnly Period, nullable VariationPct
- Infrastructure tests (IndexRepositoryTests): 6 tests — AddAsync+persist, GetByPeriodAsync null, ExistsAsync, GetLastAvailableAsync (most recent), GetRangeAsync (type+range), idempotency check

## Migration: 20260413101011_AddIndexValues

Columns created in `index_values` table:

| Column | Type | Notes |
|--------|------|-------|
| id | uuid | PK, default gen_random_uuid() |
| index_type | smallint | ICL=1, IPC=2 |
| period | date | First day of month |
| value | numeric(18,6) | Index value |
| variation_pct | numeric(10,6) | Nullable monthly variation |
| source | character varying(50) | "BCRA" or "INDEC" |
| fetched_at | timestamp with time zone | Default now() |

Unique index: `ix_index_values_type_period_unique` on `(index_type, period)`

## Deviations from Plan

None — plan executed exactly as written.

Tasks 01-01 (tests) and 01-02 (Domain types) were committed separately but implemented together in sequence to ensure compilation. The plan itself noted this was acceptable and preferred.

## Known Stubs

None. All files contain fully wired production code. No placeholder data flows to any rendering layer.

## Threat Flags

No new security surface beyond what the plan's threat model covered. IndexValue has no OrganizationId (T-02-01 mitigated by design), composite unique constraint generated in migration (T-02-02 mitigated), FetchedAt+Source columns present for audit trail (T-02-04 mitigated).

## Self-Check: PASSED

Files verified:
- api/src/GestionAlquileres.Domain/Enums/IndexType.cs: FOUND
- api/src/GestionAlquileres.Domain/Entities/IndexValue.cs: FOUND
- api/src/GestionAlquileres.Domain/Interfaces/Repositories/IIndexRepository.cs: FOUND
- api/src/GestionAlquileres.Infrastructure/Persistence/Configurations/IndexValueConfiguration.cs: FOUND
- api/src/GestionAlquileres.Infrastructure/Persistence/Repositories/IndexRepository.cs: FOUND
- api/src/GestionAlquileres.Infrastructure/Persistence/Migrations/20260413101011_AddIndexValues.cs: FOUND
- api/tests/GestionAlquileres.Tests/Phase2/Domain/IndexValueTests.cs: FOUND
- api/tests/GestionAlquileres.Tests/Phase2/Infrastructure/IndexRepositoryTests.cs: FOUND

Commits verified: 495da39, 628d9b5, e44e9a2, 743ee4f, 2b76eb7 — all present in git log.

Build: `dotnet build` — 0 errors, 0 warnings.
Tests: 11/11 Phase2 tests pass.
