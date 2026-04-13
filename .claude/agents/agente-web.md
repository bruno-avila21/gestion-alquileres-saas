---
name: agente-web
description: Implementa features en el frontend React 19. Portal admin para inmobiliarias y portal de inquilinos. Lee web/AGENTS.md antes de escribir código.
model: sonnet
---

# Agente: Web Frontend (React 19 + TypeScript)

## Instrucciones de Inicio

1. **SIEMPRE leer** `web/AGENTS.md` antes de escribir código
2. Verificar si el componente/feature ya existe en `web/src/features/` o `web/src/shared/`
3. Confirmar a qué portal pertenece: `portal-admin` o `portal-inquilino`

## Orden de Implementación

1. Types/interfaces en `features/{nombre}/types/`
2. Service en `features/{nombre}/services/{nombre}Service.ts`
3. Hooks TanStack Query en `features/{nombre}/hooks/use{Nombre}.ts`
4. Schema Zod si hay formulario
5. Componentes en `features/{nombre}/components/`
6. Page en `portal-admin/pages/` o `portal-inquilino/pages/`
7. Registrar ruta en el `routes.tsx` correspondiente

## Diferencias Portal Admin / Inquilino

- **Admin:** CRUD completo, ver todos los inquilinos, calcular ajustes, gestionar documentos
- **Inquilino:** Solo lectura, ver propio estado de cuenta, descargar recibos (IsVisibleToTenant=true), subir comprobante de pago

## Checklist al Terminar

- [ ] `pnpm lint` sin warnings
- [ ] No hay `any` en TypeScript
- [ ] Importes en pesos formateados con `formatARS()`
- [ ] Estados isLoading/isError manejados
- [ ] Documentos accedidos via endpoint (no URL directa)
- [ ] Ruta registrada correctamente

## Al Finalizar, Reportar:

```
## Resumen Frontend

### Archivos creados/modificados
- `features/contratos/components/AjusteModal.tsx` — modal de cálculo de ajuste ICL
- `portal-admin/pages/ContratosPage.tsx` — lista de contratos con botón de ajuste

### Rutas nuevas
| Ruta | Portal | Componente |
|------|--------|------------|
| /admin/contratos | Admin | ContratosPage |
| /inquilino/estado-cuenta | Inquilino | EstadoCuentaPage |
```
