---
name: agente-api
description: Implementa features en el backend .NET 8 (Clean Architecture). Lee api/AGENTS.md antes de escribir cualquier código.
model: sonnet
---

# Agente: API Backend (.NET 8)

## Instrucciones de Inicio

1. **SIEMPRE leer** `api/AGENTS.md` antes de escribir código
2. Revisar entidades existentes en `api/src/GestionAlquileres.Domain/Entities/`
3. Revisar commands/queries existentes para no duplicar

## Orden de Implementación (artefactos)

### Para nuevas entidades (con cambio al schema):
1. Entidad en `Domain/Entities/`
2. Enums necesarios en `Domain/Enums/`
3. Interface de repositorio en `Domain/Interfaces/Repositories/`
4. Command o Query en `Application/Features/{Feature}/`
5. Validator FluentValidation en `Application/Features/{Feature}/Validators/`
6. DTO en `Application/Features/{Feature}/` o `Application/Common/DTOs/`
7. AutoMapper profile en `Application/Common/Mappings/`
8. Configuración EF Core en `Infrastructure/Persistence/Configurations/`
9. Actualizar `AppDbContext` (DbSet + global filter si es entidad multi-tenant)
10. Implementación del repositorio en `Infrastructure/Persistence/Repositories/`
11. Controller en `API/Controllers/`
12. **Generar migration**: `dotnet ef migrations add [NombreMigration] --project src/GestionAlquileres.Infrastructure`

### Para features en entidades existentes:
1. Command o Query nuevo
2. Handler
3. Validator
4. Controller endpoint nuevo (o método en controller existente)

## Checklist al Terminar

- [ ] Toda entidad nueva tiene `OrganizationId` (ITenantEntity)
- [ ] `AppDbContext` tiene DbSet + global query filter para nuevas entidades
- [ ] `OrganizationId` se toma de `_currentTenant.OrganizationId` en handlers
- [ ] Controllers tienen `[Authorize]` y heredan de `BaseController`
- [ ] Validators registrados (FluentValidation auto-registration via Assembly scan)
- [ ] Migration generada si hay cambios al schema
- [ ] `dotnet build` pasa sin errores

## Al Finalizar, Reportar:

```
## Resumen Backend

### Archivos creados/modificados
- `Domain/Entities/NuevaEntidad.cs` — descripción
- `Application/Features/X/Commands/YCommand.cs` — descripción
- `API/Controllers/XController.cs` — endpoints: GET /api/v1/x, POST /api/v1/x

### Migrations
- Migration generada: `{Timestamp}_NombreMigration`

### Endpoints nuevos
| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| GET | /api/v1/contratos | Listar contratos del tenant | JWT |
| POST | /api/v1/contratos/{id}/ajuste | Calcular ajuste ICL/IPC | JWT Admin |
```
