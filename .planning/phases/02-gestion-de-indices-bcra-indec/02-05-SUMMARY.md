---
phase: 02-gestion-de-indices-bcra-indec
plan: "05"
subsystem: web-admin-indexes-ui
tags: [web, react, tanstack-query, react-hook-form, zod, indexes, admin-portal, phase2]
dependency_graph:
  requires:
    - 02-04 (API endpoints GET /indexes and POST /indexes/sync)
  provides:
    - /admin/indexes page with ICL/IPC filter, date range, table, sync dialog
    - indexService (GET /indexes, POST /indexes/sync)
    - useIndexes TanStack Query hook
    - useSyncIndex TanStack Mutation hook
    - IndexTable component
    - SyncIndexDialog component
  affects:
    - portal-admin/routes.tsx (new /indexes route)
    - portal-admin/layouts/AdminLayout.tsx (nav bar added)
tech_stack:
  added: []
  patterns:
    - TanStack Query useQuery with staleTime 30s for index list
    - TanStack Mutation with onSuccess cache invalidation for sync
    - react-hook-form + zod schema validation in dialog form
    - Native <dialog> element for accessible modal without external dependencies
    - Intl.NumberFormat('es-AR') for value and percentage formatting
    - AdminLayout role-based redirect (T-02-24 mitigated)
key_files:
  created:
    - web/src/features/indexes/types/index.types.ts
    - web/src/features/indexes/services/indexService.ts
    - web/src/features/indexes/hooks/useIndexes.ts
    - web/src/features/indexes/hooks/useSyncIndex.ts
    - web/src/features/indexes/components/IndexTable.tsx
    - web/src/features/indexes/components/SyncIndexDialog.tsx
    - web/src/features/indexes/__tests__/useSyncIndex.test.tsx
    - web/src/features/indexes/__tests__/IndexTable.test.tsx
    - web/src/portal-admin/pages/IndexesPage.tsx
  modified:
    - web/src/portal-admin/routes.tsx (added indexes route)
    - web/src/portal-admin/layouts/AdminLayout.tsx (added nav bar with Dashboard + Indices links)
decisions:
  - "Native <dialog> element used for SyncIndexDialog — no shadcn Dialog installed; avoids external dependency and provides accessibility (backdrop, Esc-to-close) out of the box"
  - "defaultIndexType prop added to SyncIndexDialog to sync selected IndexType from IndexesPage into the dialog pre-selection"
  - "staleTime 30s on useIndexes — index data changes infrequently; avoids redundant refetches while keeping data reasonably fresh"
  - "invalidateQueries({queryKey: ['indexes']}) on sync success — broad invalidation ensures any active query for any date range / type refetches"
metrics:
  duration: "~15 minutes"
  completed: "2026-04-13"
  tasks_completed: 3
  files_created: 9
  files_modified: 2
---

# Phase 2 Plan 05: Admin Portal UI for Index Management Summary

React feature `indexes` with TanStack Query hooks, IndexTable + SyncIndexDialog components, and IndexesPage wired at `/admin/indexes` behind AdminLayout. 8 new tests (6 IndexTable + 2 useSyncIndex). TypeScript strict, no `any`, pnpm build 0 errors.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 05-01 | Scaffold indexes feature (types, service, hooks) with tests | 6b260ad | 5 files created |
| 05-02 | Build IndexTable + SyncIndexDialog components with tests | 6519c83 | 3 files created |
| 05-03 | Build IndexesPage + wire into admin routes + add nav link | 9ec1870 | 1 created, 2 modified |
| 05-04 | Manual UI verification | — | Awaiting human checkpoint |

## Test Results

| Suite | Tests | Status |
|-------|-------|--------|
| useSyncIndex.test.tsx | 2 | Passing |
| IndexTable.test.tsx | 6 | Passing |
| LoginForm.test.tsx (existing) | 3 | Passing |
| **Total** | **11** | **All green** |

## Architecture

**Feature structure (features/indexes/):**
- `types/index.types.ts` — IndexType, IndexValueDto, SyncIndexRequest, SyncIndexResult, ApiErrorBody
- `services/indexService.ts` — list() and sync() using shared axios `api` client
- `hooks/useIndexes.ts` — queryKey `['indexes', type, from, to]`, staleTime 30s
- `hooks/useSyncIndex.ts` — mutation, invalidates `['indexes']` on success
- `components/IndexTable.tsx` — loading/error/empty/data states, es-AR formatting
- `components/SyncIndexDialog.tsx` — native `<dialog>`, zod-validated form, 3 result states

**Portal wiring:**
- `portal-admin/pages/IndexesPage.tsx` — ICL/IPC toggle, date range, table, sync dialog
- `portal-admin/routes.tsx` — `{ path: 'indexes', element: <IndexesPage /> }` under AdminLayout
- `portal-admin/layouts/AdminLayout.tsx` — nav bar with Dashboard + Indices NavLink

## SyncIndexDialog Result States

| Condition | UI Feedback |
|-----------|-------------|
| `wasFallback === true` | Yellow warning banner with fallback period |
| `alreadyExisted === true` | Blue info banner "Ese período ya estaba sincronizado" |
| Normal success | Green success banner with indexType + period + value |
| Error (400/409/5xx) | Red alert in dialog, dialog stays open |
| isPending | Button disabled + "Sincronizando…" label (T-02-28) |

## Deviations from Plan

### Auto-fixed Issues

None — plan executed exactly as written. `defaultIndexType` prop was anticipated in the plan's note ("add optional prop in Task 05-02") and implemented as directed.

## Known Stubs

None. All data flows from real API calls via indexService. No hardcoded values or placeholder text in production code paths.

## Threat Flags

All threats in the plan's threat model are mitigated:

| Threat ID | Mitigation Applied |
|-----------|-------------------|
| T-02-24 | AdminLayout checks `user.role === 'Tenant'` and redirects |
| T-02-25 | Zod schema: year <= currentYear, month 1..12 in SyncIndexDialog |
| T-02-26 | Error message rendered as React text child (no innerHTML) |
| T-02-27 | React auto-escapes all string table values |
| T-02-28 | Button disabled during `isPending`; text changes to "Sincronizando…" |
| T-02-29 | No change from existing pattern — accepted carry-over |

## Self-Check: PASSED

Files verified present:
- web/src/features/indexes/types/index.types.ts: FOUND
- web/src/features/indexes/services/indexService.ts: FOUND
- web/src/features/indexes/hooks/useIndexes.ts: FOUND
- web/src/features/indexes/hooks/useSyncIndex.ts: FOUND
- web/src/features/indexes/components/IndexTable.tsx: FOUND
- web/src/features/indexes/components/SyncIndexDialog.tsx: FOUND
- web/src/features/indexes/__tests__/useSyncIndex.test.tsx: FOUND
- web/src/features/indexes/__tests__/IndexTable.test.tsx: FOUND
- web/src/portal-admin/pages/IndexesPage.tsx: FOUND

Commits verified: 6b260ad, 6519c83, 9ec1870 — all present in git log.

Build: 0 TypeScript errors, Vite build 167 modules transformed.
Tests: 11/11 passing (3 existing + 8 new).
