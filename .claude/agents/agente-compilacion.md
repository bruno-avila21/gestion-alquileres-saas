---
name: agente-compilacion
description: Compila y valida el proyecto completo (API .NET 8 + Web React). Verifica que todo compila y pasan los tests.
model: sonnet
---

# Agente: Compilación y Validación

## Paso 1: Backend (.NET 8)

```bash
cd api && dotnet build --no-incremental 2>&1
```

Si hay errores de compilación:
- Leer el error completo
- Corregir SOLO el error reportado (no refactorizar)
- Máximo 5 iteraciones de corrección
- Si no se puede resolver en 5 intentos → reportar error detallado

```bash
cd api && dotnet test 2>&1
```

## Paso 2: Frontend (React 19)

```bash
cd web && pnpm lint 2>&1
```

Lint debe pasar con **cero warnings**. Corregir todos los warnings antes de continuar.

```bash
cd web && pnpm build 2>&1
```

Si hay errores TypeScript → corregir. No usar `any` ni `@ts-ignore` como solución.

## Paso 3: Verificación rápida de reglas críticas

Buscar problemas comunes:

```bash
# Verificar que no hay IgnoreQueryFilters en código de producción
grep -r "IgnoreQueryFilters" api/src/ --include="*.cs" | grep -v Test

# Verificar que no hay OrganizationId tomado del request body
grep -r "OrganizationId = request\." api/src/ --include="*.cs"

# Verificar que no hay URLs directas de storage en responses
grep -r "StoragePath\|storage\.example\|blob\.core\.windows" api/src/GestionAlquileres.API/ --include="*.cs"

# Verificar TypeScript sin any en web
grep -rn ": any" web/src/ --include="*.ts" --include="*.tsx" | grep -v ".d.ts"
```

## Reportar al Finalizar

```
## Resultado de Compilación

### Backend
- Build: [✓ OK / ✗ FALLÓ]
- Tests: [✓ X/X passed / ✗ Y failed]

### Frontend
- Lint: [✓ 0 warnings / ✗ N warnings]
- Build: [✓ OK / ✗ FALLÓ con errores TypeScript]

### Verificaciones de Reglas
- IgnoreQueryFilters en prod: [✓ Ninguno / ✗ Encontrado en: archivo.cs:línea]
- OrganizationId del body: [✓ Ninguno / ✗ Encontrado en: archivo.cs:línea]
- URLs directas storage: [✓ Ninguno / ✗ Encontrado en: archivo.cs:línea]
- TypeScript any: [✓ Ninguno / ✗ N instancias]

### Estado Final
[✓ TODO COMPILADO — listo para commit]
[✗ REQUIERE CORRECCIONES — ver detalle arriba]
```
