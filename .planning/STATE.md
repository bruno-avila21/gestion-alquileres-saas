# Project State — Gestión Alquileres SaaS

## Project Reference

See: .planning/PROJECT.md (updated 2026-04-12)

**Core value:** Cálculo automático de ajustes ICL/IPC con datos BCRA persistidos — confiable, auditable, multi-tenant
**Current focus:** Phase 1 — Scaffolding & Multi-tenancy Foundation

## Current Status

- Phase 1: ○ Pending (ready to start)
- Phase 2: ○ Pending
- Phase 3: ○ Pending
- Phase 4: ○ Pending
- Phase 5: ○ Pending
- Phase 6: ○ Pending
- Phase 7: ○ Pending
- Phase 8: ○ Pending

Progress: ░░░░░░░░░░ 0%

## Active Decisions

- Stack confirmado: .NET 8 + React 19 + PostgreSQL + pnpm
- Multi-tenancy: shared DB con OrganizationId discriminador
- Storage: MinIO para desarrollo, Azure Blob para producción

## Last Session

2026-04-12 — Inicialización del proyecto. Claude Code configurado completo.
ERD diseñado. Requirements y Roadmap creados.

## Notes

- Arrancar con: `/gsd-plan-phase 1`
- API del BCRA: https://api.bcra.gob.ar (requiere investigar endpoints exactos en Phase 2)
- Considerar Docker Compose para desarrollo local: PostgreSQL + MinIO + Hangfire dashboard
