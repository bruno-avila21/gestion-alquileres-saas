---
phase: 1
slug: scaffolding-multi-tenancy-foundation
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-12
---

# Phase 1 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) + vitest (React) |
| **Config file** | api/GestionAlquileres.sln / web/vite.config.ts |
| **Quick run command** | `cd api && dotnet test --no-build --filter "Category=Unit"` |
| **Full suite command** | `cd api && dotnet test && cd ../web && pnpm test` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `cd api && dotnet build --no-restore`
- **After every plan wave:** Run `cd api && dotnet test && cd ../web && pnpm build`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 60 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 1-01-01 | 01 | 1 | INFRA-03 | — | N/A | build | `cd api && dotnet build` | ❌ W0 | ⬜ pending |
| 1-01-02 | 01 | 1 | INFRA-01 | — | N/A | build | `cd api && dotnet build` | ❌ W0 | ⬜ pending |
| 1-01-03 | 01 | 2 | INFRA-02 | T-1-01 | OrganizationId filter isolates tenant data | unit | `cd api && dotnet test` | ❌ W0 | ⬜ pending |
| 1-01-04 | 01 | 2 | ORG-04 | T-1-02 | JWT with OrganizationId claim returns 200 | unit | `cd api && dotnet test` | ❌ W0 | ⬜ pending |
| 1-02-01 | 02 | 3 | ORG-01..03 | T-1-03 | register-org creates org + admin user | integration | `cd api && dotnet test` | ❌ W0 | ⬜ pending |
| 1-03-01 | 03 | 4 | N/A | — | N/A | build | `cd web && pnpm build` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `api/tests/GestionAlquileres.Tests/` — test project structure
- [ ] `api/tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj` — xUnit project
- [ ] `web/src/__tests__/` — vitest test directory

*If none: "Existing infrastructure covers all phase requirements."*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Login page visual + UX flow | ORG-04 | UI/browser interaction | Open browser at localhost:5173, verify login form, submit credentials, check JWT stored |
| Multi-tenancy isolation end-to-end | INFRA-02 | Requires two orgs in DB | Register 2 orgs, login as each, verify data isolation |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
