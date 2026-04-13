---
name: desarrollo
description: >
  Desarrollo fullstack de features del SaaS de alquileres. Trigger: al implementar
  nuevas funcionalidades, endpoints, pantallas o flujos de negocio (ajuste ICL/IPC,
  gestión de contratos, portal inquilino, documentos).
metadata:
  version: "1.0"
---

# Skill: Desarrollo Fullstack — Gestión Alquileres SaaS

## Flujo

```
PASO 0: Analizar el requerimiento
PASO 1: Identificar impacto en capas y generar plan
PASO 2: Esperar aprobación del usuario
PASO 3: Backend — Domain + Application (Clean Architecture)
PASO 4: Backend — Infrastructure + API + Migration
PASO 5: Frontend — Services + Hooks + Componentes
PASO 6: Compilar y validar (agente-compilacion)
PASO 7: Resumen de lo creado
```

## PASO 0: Analizar

Leer el input. Si es texto libre, identificar:
- Entidades afectadas (Contrato, Propiedad, Inquilino, RentHistory, IndexValue, Transaction, Document)
- Tipo de feature: CRUD, cálculo de ajuste, sincronización BCRA, portal inquilino, documentos
- Impacto en schema (¿nuevas tablas o columnas? → migration necesaria)
- Afecta a portal admin, portal inquilino, o ambos

## PASO 1: Planificar

Entrar en modo Plan. Listar:

**Backend:**
- Entidades nuevas/modificadas en Domain
- Commands/Queries en Application
- Cambios en Infrastructure (EF, repos, servicios externos)
- Endpoints nuevos en API
- ¿Necesita migration EF Core? (Sí/No + descripción del cambio)

**Frontend:**
- Services API afectados
- Hooks nuevos
- Componentes/pages nuevos
- ¿Portal admin, inquilino, o ambos?

**Reglas de negocio críticas a implementar:**
- ICL: fórmula trimestral con índice persistido (no live)
- IPC: acumulación según frecuencia del contrato
- Documentos: IsVisibleToTenant flag + presigned URLs
- Multi-tenancy: OrganizationId del JWT

Esperar aprobación del usuario.

## PASO 3: Backend — Domain + Application

Delegar a `agente-api` con:
- Entidades y Value Objects a crear
- Commands/Queries/Handlers/Validators a implementar
- Reglas de negocio específicas (incluir fórmulas exactas si aplica)
- Interfaces de repositorio si son nuevas

## PASO 4: Backend — Infrastructure + API

Delegar a `agente-api` (segunda oleada) con:
- Configuraciones EF Core para las entidades de la oleada anterior
- Actualización de AppDbContext (DbSets + global filters)
- Implementaciones de repositorios
- Controllers y endpoints nuevos
- Instrucción explícita: "generar migration si hay cambios al schema"

## PASO 5: Frontend

Delegar a `agente-web` con:
- Endpoints disponibles (método + ruta + tipos de request/response)
- Lógica de negocio relevante para UI
- Portal destino (admin / inquilino / ambos)
- Restricciones del portal inquilino si aplica (solo ver propios datos, solo documentos visibles)

## PASO 6: Compilar

Delegar a `agente-compilacion`.

## PASO 7: Resumen

```markdown
## Resumen de Feature Implementada

### Feature
[Nombre y descripción breve]

### Backend — Archivos creados
- [ruta] — [descripción]
...

### Backend — Endpoints nuevos
| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|

### Frontend — Archivos creados
- [ruta] — [descripción]

### Migrations
- [Nombre de la migration si aplica]

### Reglas de negocio implementadas
- [Describir brevemente la lógica crítica: fórmula ICL, manejo de índices, etc.]
```
