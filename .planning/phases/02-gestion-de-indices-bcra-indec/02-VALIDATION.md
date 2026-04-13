---
phase: 2
slug: gestion-de-indices-bcra-indec
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-13
---

# Phase 2 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit + FluentAssertions + WireMock.Net |
| **Config file** | `api/tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj` |
| **Quick run command** | `cd api && dotnet test --filter "Phase2" --no-build` |
| **Full suite command** | `cd api && dotnet test` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `cd api && dotnet test --filter "Phase2" --no-build`
- **After every plan wave:** Run `cd api && dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 2-01-01 | 01 | 1 | IDX-03 | — | N/A | unit | `dotnet test --filter "IndexValue"` | ❌ W0 | ⬜ pending |
| 2-01-02 | 01 | 1 | IDX-03 | — | N/A | unit | `dotnet test --filter "IIndexRepository"` | ❌ W0 | ⬜ pending |
| 2-02-01 | 02 | 1 | IDX-01 | — | N/A | integration | `dotnet test --filter "BcraApiClient"` | ❌ W0 | ⬜ pending |
| 2-02-02 | 02 | 1 | IDX-02 | — | N/A | integration | `dotnet test --filter "IndecApiClient"` | ❌ W0 | ⬜ pending |
| 2-02-03 | 02 | 1 | IDX-04 | — | Fallback on API failure | integration | `dotnet test --filter "Fallback"` | ❌ W0 | ⬜ pending |
| 2-03-01 | 03 | 2 | IDX-01,IDX-03 | — | N/A | integration | `dotnet test --filter "SyncIndex"` | ❌ W0 | ⬜ pending |
| 2-03-02 | 03 | 2 | IDX-06 | — | N/A | integration | `dotnet test --filter "GetIndexByPeriod"` | ❌ W0 | ⬜ pending |
| 2-04-01 | 04 | 2 | IDX-05,IDX-06 | — | N/A | integration | `dotnet test --filter "IndexesController"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `api/tests/GestionAlquileres.Tests/Phase2/Domain/IndexValueTests.cs` — stubs for IDX-03 entity
- [ ] `api/tests/GestionAlquileres.Tests/Phase2/Infrastructure/BcraApiClientTests.cs` — stubs for IDX-01, IDX-04
- [ ] `api/tests/GestionAlquileres.Tests/Phase2/Infrastructure/IndecApiClientTests.cs` — stubs for IDX-02
- [ ] `api/tests/GestionAlquileres.Tests/Phase2/Application/SyncIndexCommandHandlerTests.cs` — stubs for IDX-01..04
- [ ] `api/tests/GestionAlquileres.Tests/Phase2/Application/GetIndexByPeriodQueryHandlerTests.cs` — stubs for IDX-06
- [ ] `api/tests/GestionAlquileres.Tests/Phase2/API/IndexesControllerTests.cs` — stubs for IDX-05, IDX-06

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| BCRA API live sync returns real ICL values | IDX-01 | External API — not mocked in automated tests | Run `POST /api/v1/indexes/sync` and verify DB row with current ICL value |
| INDEC API live sync returns real IPC values | IDX-02 | External API — not mocked in automated tests | Run `POST /api/v1/indexes/sync` and verify DB row with current IPC value |
| Frontend table displays synced index values | (Phase 2 frontend) | Requires running browser + dev server | Navigate to `/indexes` in portal admin, verify table shows rows with period, type, value |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
