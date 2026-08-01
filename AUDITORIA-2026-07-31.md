# Auditoría consolidada — 2026-07-31

**Proyecto:** SaaS multi-tenant de gestión de alquileres (api/ .NET 8 · web/ React 19)
**Rama:** `fix/audit-2026-07-18`
**Modo:** solo lectura — ningún archivo del proyecto fue modificado por los auditores.
**Contexto:** el próximo objetivo de producto es una app mobile para el portal de inquilinos y el de administradores/propietarios.

---

## 0. Estado de la auditoría (leer primero)

Se lanzaron 5 auditores en paralelo. **Cuatro se cortaron por límite de sesión de la API**, pero varios de sus subagentes alcanzaron a terminar y entregaron material verificado.

| Área | Estado | Fuente |
|---|---|---|
| Producto / UX / Accesibilidad | ✅ **Completa** — 71 hallazgos sobre `web/src` entero | Agente 3 |
| Infraestructura: jobs, servicios externos, storage, email, scheduler | ✅ **Completa** | Subagente del Agente 4 |
| Persistencia: migraciones, esquema, índices, concurrencia | ✅ **Completa** — 12 migraciones, 11 tablas, 30 índices, 18 FKs | Subagente del Agente 4 |
| Cobertura de tests de la lógica crítica ICL/IPC | ✅ **Completa** — con bugs reales de producción detectados | Subagente del Agente 2 |
| Superficie de API: endpoints, DTOs, serialización | ✅ **Completa** — inventario exhaustivo | Subagente del Agente 3 |
| Seguridad (auditoría dedicada) | ✅ **Completa** — 17 hallazgos, 3 altos | Agente 1 — relanzado |
| Bugs y correctitud (auditoría dedicada) | ✅ **Completa** — 21 hallazgos, 1 crítico | Agente 2 — relanzado |
| **Arquitectura (síntesis)** | ⚠️ **Parcial** — datos crudos sí, síntesis no | Agente 4 — cortado |
| Mercado y estrategia | ✅ **Completa** — ver §7bis | Agente 5 — relanzado |

Todo lo que sigue está verificado contra el código con `archivo:línea`. Lo que no se pudo verificar está en §8.

---

## 1. Resumen ejecutivo — los 10 que hay que atacar primero

Ordenados por daño real × facilidad de arreglo.

| # | Hallazgo | Ubicación | Nivel |
|---|---|---|---|
| 0 | ~~**Autorización aplicada después de escribir**: se puede hacer visible un documento de otro contrato al inquilino equivocado~~ | `DocumentsController.cs:52-53` | ✅ **CORREGIDO** 2026-07-31 |
| 1 | ~~Los documentos se pierden en cada reinicio~~ → **diagnóstico corregido**: producción no puede arrancar con storage local (`SecuritySettingsValidator.cs:62-71`). El bug real era `BasePath: ""` rompiendo el upload en desarrollo | `appsettings.json:29` · `LocalFileStorageService.cs:12` | ✅ **CORREGIDO** 2026-07-31 |
| 2 | ~~El job de vencimientos manda el mismo mail todos los días durante 30 días~~ | `ContractExpiryNotificationJob.cs:9,29-30` | ✅ **CORREGIDO** 2026-07-31 |
| 3 | División por cero en el cálculo de ajuste si el índice base vale 0 | `ApplyRentAdjustmentCommandHandler.cs:125-126` | 🔴 Crítico |
| 4 | ~~Todas las fechas del frontend se muestran un día antes en Argentina~~ | `web/src/shared/lib/formatters.ts:6-7` | ✅ **CORREGIDO** 2026-07-31 |
| 5 | El token de descarga de documentos viaja en query string y se loguea | `DocumentTokenService.cs:22-26` · `Program.cs:195` | 🔴 Crítico |
| 6 | `MonthlyRentAdjustmentJob` — el corazón del negocio — no tiene ni un test | `api/src/GestionAlquileres.API/Jobs/MonthlyRentAdjustmentJob.cs:32` | 🔴 Crítico |
| 7 | Toda réplica de la API se vuelve worker de Hangfire, sin flag | `Program.cs:166` | 🟠 Alto |
| 8 | La fecha efectiva de un ajuste usa UTC, no hora argentina | `ApplyRentAdjustmentCommandHandler.cs:64` | 🟠 Alto |
| 9 | No se puede editar un contrato: el botón existe y no hace nada | `web/src/portal-admin/pages/ContratoDetailPage.tsx:553` | 🟠 Alto |
| 10 | Las dos pantallas de login son inusables en un teléfono | `portal-admin/pages/LoginPage.tsx:36-42` · `portal-inquilino/pages/LoginPage.tsx:34-40` | 🟠 Alto |

---

## 2. Infraestructura, jobs y servicios externos

### ⚠️ CORREGIDO EL DIAGNÓSTICO (2026-07-31) — no era "se pierden en producción"

> **La severidad original estaba sobredimensionada.** `SecuritySettingsValidator.cs:62-71` **ya rechaza el arranque** con `Storage:Provider != S3` fuera de Development, y está conectado en `Program.cs:99`. O sea: producción **no puede bootear** con storage local — falla ruidosamente en vez de perder documentos en silencio. El control ya existía y la auditoría de infraestructura no lo detectó.
>
> **Lo que sí estaba roto, y se arregló:** `appsettings.json:29` trae `"BasePath": ""`, y un string vacío **no es null**, así que el `??` nunca se activaba y `Directory.CreateDirectory("")` tiraba `ArgumentException`. En un clone limpio, el primer upload en desarrollo se caía. Ahora se trata el blanco como "sin configurar" y se agregó la sección `Storage` al `.example` apuntando al MinIO del compose, para que desarrollo ejercite el mismo camino que producción.
> También se agregó la canonicalización de path que faltaba (`ResolvePath`), cerrando el hallazgo de traversal de §3.
> Cobertura: `Phase7/LocalFileStorageServiceTests.cs` — 7 tests (BasePath en blanco, round-trip, traversal por `../` y por ruta absoluta, en descarga y borrado).

### ~~🔴 CRÍTICO — Los documentos subidos se pierden en cada redeploy~~ (ver corrección arriba)

- **Ubicación:** `api/src/GestionAlquileres.Infrastructure/DependencyInjection.cs:89-94`, `api/src/GestionAlquileres.API/appsettings.json:28`, `api/src/GestionAlquileres.Infrastructure/Storage/LocalFileStorageService.cs:12-14`, `Dockerfile:17`
- **Evidencia:**
```csharp
var provider = configuration["Storage:Provider"] ?? "Local";
if (!provider.Equals("S3", StringComparison.OrdinalIgnoreCase))
{
    services.AddScoped<IStorageService, LocalFileStorageService>();
    return;
}
```
```csharp
_basePath = configuration["Storage:BasePath"]
    ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
```
- **Impacto:** `appsettings.json:28` fija `"Provider": "Local"` y ni `appsettings.Development.json` ni `docker-compose.yml` lo cambian a S3 (el compose levanta MinIO en las líneas 19-35 y crea el bucket en 37-48, **pero no define un servicio `api` ni ninguna variable `Storage__Provider`**). Resultado: la API escribe en `/app/uploads` (`Dockerfile:17`), una capa de contenedor sin volumen montado. Todo contrato firmado, recibo y comprobante desaparece en el próximo deploy, y es invisible para cualquier otra réplica.
- **Agravante:** `appsettings.json:29` define `"BasePath": ""`. Como `""` no es `null`, el `??` no se activa y `Directory.CreateDirectory("")` lanza `ArgumentException` en el constructor del servicio.
- **Remediación:** activar S3/MinIO por configuración en todos los entornos no-test, o montar un volumen persistente. Y cambiar el `??` por `string.IsNullOrWhiteSpace`.

### 🔴 CRÍTICO — El job de vencimientos reenvía el mismo email 30 días seguidos

- **Ubicación:** `api/src/GestionAlquileres.API/Jobs/ContractExpiryNotificationJob.cs:9`, `:29-30`, `:40`
- **Evidencia:** la clase no tiene `[DisableConcurrentExecution]` (comparar con `MonthlyRentAdjustmentJob.cs:15` y `SyncIndexesJob.cs:11`, que sí lo tienen), y la ventana es:
```csharp
.Where(c => c.Status == ContractStatus.Active && c.EndDate >= today && c.EndDate <= cutoff)
```
- **Impacto:** no existe columna `NotificationSent` ni ningún marcador de envío. El job corre todos los días a las 10:00 (`Program.cs:251`) y **todo contrato dentro de la ventana de 30 días recibe el mismo mail cada día, hasta 30 veces**. Sin lock distribuido, un reintento o un disparo manual desde el dashboard duplica los envíos dentro del mismo día. Es el camino más rápido a que la inmobiliaria termine en la carpeta de spam de sus propios inquilinos.
- **Remediación:** marcador de envío persistido (columna o tabla de notificaciones) + `[DisableConcurrentExecution]`.

### 🟠 ALTO — Cada réplica de la API se convierte en worker de Hangfire

- **Ubicación:** `api/src/GestionAlquileres.API/Program.cs:166`
- **Evidencia:** `builder.Services.AddHangfireServer();` — sin condición de entorno, sin flag `Hangfire:RunServer`, sin entrypoint de worker separado.
- **Impacto:** con N réplicas hay N × `ProcessorCount` × 5 workers compitiendo sobre el mismo Postgres, que en dev es **la misma base que la app** (`appsettings.Development.json:3-4`), compartiendo el pool de conexiones. Además el host de tests (`WebApplicationFactory`, `Program.cs:292`) también levanta un worker.
- **Remediación:** flag de configuración, o proceso worker separado.

### 🟠 ALTO — `MonthlyRentAdjustmentJob` carga todos los contratos activos del planeta en memoria

- **Ubicación:** `api/src/GestionAlquileres.Infrastructure/Persistence/Repositories/ContractRepository.cs:55-61`
- **Evidencia:**
```csharp
await _db.Contracts
    .IgnoreQueryFilters()
    .Include(c => c.Property)
    .Include(c => c.AppTenant)
    .Where(c => c.Status == ContractStatus.Active)
    .ToListAsync(ct);
```
- **Impacto:** sin paginación, sin `AsNoTracking()`, con dos eager loads. El pico de memoria crece linealmente con el total de contratos activos de toda la plataforma. Mismo patrón en `GetExpiringRawAsync` (`:63-73`).
- **Atenuante:** se proyecta inmediatamente a un struct chico y se descarta el DbContext (`MonthlyRentAdjustmentJob.cs:40-49`), y cada contrato se procesa en su propio scope (`:74`).
- **Remediación:** paginar por org o por lotes + `AsNoTracking()`.

### 🟠 ALTO — Los errores de los jobs se tragan: Hangfire los reporta como exitosos

- **Ubicación:** `MonthlyRentAdjustmentJob.cs:60-64`, `SyncIndexesJob.cs:53-58`, `ContractExpiryNotificationJob.cs:52-57`
- **Evidencia:** try/catch por ítem que loguea y continúa. Un `grep AutomaticRetry` sobre todo el repo **no devuelve nada**.
- **Impacto:** si fallan los 500 ajustes del mes, el job termina "OK". Sin reintento automático, sin alerta más allá de una línea de log. El único job que deja funcionar el retry de Hangfire es `RefreshTokenCleanupJob` (`:11`), porque no tiene try/catch.
- **Agravante relacionado:** el registro de los recurring jobs está dentro de un try/catch que solo loguea un `Warning` (`Program.cs:263-265`) — si el registro falla, **el job simplemente nunca corre y nadie se entera**.

### 🟠 ALTO — Los jobs no pueden cancelarse y corren en el huso horario equivocado

- **Ubicación:** `Program.cs:243-261`
- **Evidencia:** todos los `RecurringJob.AddOrUpdate` pasan `CancellationToken.None` en el registro, y ningún `Cron.*` recibe `TimeZoneInfo`.
- **Impacto:** (a) el `ct` que llega a cada job es siempre `None`, así que los chequeos de cancelación (`MonthlyRentAdjustmentJob.cs:55`) son código muerto y no hay apagado ordenado; (b) el cron se evalúa en el huso del servidor mientras el cuerpo del job calcula fechas con `ArgentinaTime.Today` (`api/src/GestionAlquileres.Application/Common/Time/ArgentinaTime.cs:17`) — en un contenedor UTC, el "09:00" se dispara a las 06:00 de Argentina.

### 🟠 ALTO — Timeout del HttpClient mal configurado: anula la resiliencia

- **Ubicación:** `api/src/GestionAlquileres.Infrastructure/DependencyInjection.cs:69` vs `:76`
- **Evidencia:** `client.Timeout = TimeSpan.FromSeconds(30)` contra `options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60)`.
- **Impacto:** `HttpClient.Timeout` envuelve todo el pipeline de resiliencia, así que el timeout total de 60s **nunca se alcanza**: con 3 reintentos × (10s + 2s) la llamada se corta a los 30s con un `TaskCanceledException` crudo en vez de un `TimeoutRejectedException` manejable. El circuit breaker queda con los defaults de la librería, sin configurar explícitamente.
- **Nota positiva:** el pipeline en sí está bien montado — `AddStandardResilienceHandler` con retry, timeouts y circuit breaker (`:71-77`), usando `Microsoft.Extensions.Http.Resilience 8.10.0`.

### 🟡 MEDIO — El fallback de índices devuelve un valor que no sirve

- **Ubicación:** `api/src/GestionAlquileres.Application/Features/Indexes/Commands/SyncIndexCommandHandler.cs:60-75`
- **Evidencia:** ante caída de la API externa se devuelve `SyncIndexResults.Fallback(ToDto(fallback))` con el último valor conocido — **pero no se persiste nada para el período pedido**.
- **Impacto:** el llamador recibe un valor etiquetado con el período viejo, y `ApplyRentAdjustmentCommandHandler.cs:112-118` igual falla con "No hay índice disponible para {period}". El fallback comunica éxito parcial pero no destraba el flujo. Lo bueno: **no contamina la base**, que es la decisión correcta según la regla de índices persistidos de `CLAUDE.md`.

### 🟡 MEDIO — Email: síncrono en el request, sin timeout, sin reintento, sin cola

- **Ubicación:** `api/src/GestionAlquileres.Infrastructure/Services/SmtpEmailService.cs:86-96`, `ApplyRentAdjustmentCommandHandler.cs:166-186`
- **Evidencia:** se crea un `SmtpClient` nuevo (con su handshake TCP+TLS) por cada mensaje; `SendMailAsync` no acepta `CancellationToken` y no se configura `Timeout`. En `ApplyRentAdjustmentCommandHandler` el envío ocurre **inline sobre el hilo del request HTTP**, después del commit.
- **Impacto:** un relay SMTP colgado suma su timeout completo a la latencia del endpoint de ajuste. En `ContractExpiryNotificationJob.cs:40` es una conexión por contrato, en serie. Si el envío falla, el mail se pierde: no hay reintento ni dead-letter.
- **Bien resuelto:** el fallo de email nunca revierte el ajuste (`:181-185`), que es la decisión correcta.
- **Dato:** el proveedor por defecto es `NullEmailService` (`appsettings.json:42`) — o sea que **hoy, en la configuración que está commiteada, no se envía ningún email**.

### 🟡 MEDIO — `BcraApiClient` e `IndecApiClient` son código muerto sin resiliencia

- **Ubicación:** `api/src/GestionAlquileres.Infrastructure/ExternalServices/BcraApiClient.cs:34-35`, `IndecApiClient.cs:32-33`
- **Evidencia:** un `grep AddHttpClient` sobre `api/` solo devuelve `DependencyInjection.cs:62` (el de `IndicesApiClient`). Ninguno de los dos está registrado; solo los referencian los tests (`api/tests/GestionAlquileres.Tests/Phase2/Infrastructure/BcraApiClientTests.cs:30`).
- **Impacto:** cero timeout, cero retry, cero circuit breaker, y `BcraApiClient.cs:45` lanza `FormatException` no manejada ante una fecha malformada. Si alguien los vuelve a conectar, hereda todo eso.

---

## 3. Seguridad

Los primeros cinco hallazgos salieron de la pasada de infraestructura; el resto (§3.7 en adelante) de la auditoría de seguridad dedicada.

### 🔴 CRÍTICO — El token de descarga de documentos viaja en la query string y se loguea

- **Ubicación:** `api/src/GestionAlquileres.Infrastructure/Storage/DocumentTokenService.cs:22-26`, `api/src/GestionAlquileres.API/Controllers/FilesController.cs:10`, `Program.cs:195`
- **Evidencia:**
```csharp
var exp = DateTimeOffset.UtcNow.AddMinutes(ExpiryMinutes).ToUnixTimeSeconds();
var payload = Encoding.UTF8.GetBytes($"{documentId}:{organizationId}:{exp}");
using var hmac = new HMACSHA256(_key);
return Base64UrlEncode(payload) + "." + Base64UrlEncode(sig);
```
y el endpoint que lo consume es `[AllowAnonymous]`, recibiendo `?token=...`.
- **Impacto:** `UseSerilogRequestLogging` (`Program.cs:195`) registra la URL completa, y cualquier proxy o balanceador intermedio también. El token es la **única** credencial para leer un documento privado, y queda escrito en los logs. Tampoco es revocable antes de los 5 minutos.
- **Bien hecho:** comparación en tiempo constante (`:46` `CryptographicOperations.FixedTimeEquals`), chequeo de expiración (`:53`), ventana corta de 5 min (`:11`).
- **Remediación:** mover el token al header `Authorization` o a un POST, y/o excluir la query string del logging.

### 🟠 ALTO — No hay validación de contenido real en la subida de archivos

- **Ubicación:** `api/src/GestionAlquileres.API/Controllers/DocumentsController.cs:36-37`, `api/src/GestionAlquileres.Application/Features/Documents/Commands/UploadDocumentCommandValidator.cs:11-29`
- **Evidencia:** el validador tiene whitelist de MIME types y límite de 50 MB, pero tanto `MimeType` como `SizeBytes` se toman **de lo que declara el cliente** (`MimeType: file.ContentType, SizeBytes: file.Length`). No hay verificación de magic bytes.
- **Impacto:** un ejecutable renombrado y declarado `application/pdf` se almacena y luego se re-sirve con `Content-Type: application/pdf` tomado de la base (`FilesController.cs:38`). Mitigado parcialmente por el `X-Content-Type-Options: nosniff` global (`Program.cs:201`).
- **Extra:** no hay override de `MultipartBodyLengthLimit` ni de `MaxRequestBodySize`, así que el límite efectivo son los 30 MB por defecto del framework, no los 50 MB del atributo.

### 🟠 ALTO — Path traversal sin defensa en profundidad en el storage local

- **Ubicación:** `api/src/GestionAlquileres.Infrastructure/Storage/LocalFileStorageService.cs:28`, `:37`
- **Evidencia:** `var filePath = Path.Combine(_basePath, storageKey);` en descarga y borrado, sin canonicalizar ni verificar que el resultado siga dentro de `_basePath`.
- **Impacto:** hoy `storageKey` solo se genera como GUID en `:19`, así que no es directamente atacable — pero `Path.Combine` descarta `_basePath` silenciosamente si `storageKey` viene rooteado, y falta el chequeo `Path.GetFullPath(filePath).StartsWith(_basePath)`.

### 🟠 ALTO — El dashboard de Hangfire queda sin autenticación en Development

- **Ubicación:** `api/src/GestionAlquileres.API/Program.cs:231-238`
- **Evidencia:** el filtro de autorización se construye con `app.Environment.IsDevelopment()`, e `IsReadOnlyFunc = _ => !app.Environment.IsDevelopment()`.
- **Impacto:** en Development el dashboard es **escribible y sin auth**: cualquiera puede disparar, borrar o reprogramar jobs sobre la base compartida.

### 🟡 MEDIO — Secretos de desarrollo commiteados

- **Ubicación:** `api/src/GestionAlquileres.API/appsettings.Development.json:14`
- **Evidencia:** `"ApiKey": "dev-indices-api-key"`.
- **Nota:** la API key se lee **una sola vez al arrancar** y se hornea en `DefaultRequestHeaders` (`DependencyInjection.cs:68`), así que rotarla exige reiniciar el proceso.

### 🟠 ALTO — No existe cambio ni reseteo de contraseña: la temporal del inquilino es permanente

- **Ubicación:** `InviteTenantCommandHandler.cs:34-51` · `AuthController.cs:25-105` · `Domain/Entities/User.cs:10`
- **Evidencia:** `AuthController` solo expone `register-org`, `login`, `tenant-login`, `refresh` y `logout`. No hay `change-password`, `forgot-password` ni `reset-password`. `User` no tiene `MustChangePassword` ni `PasswordChangedAt`.
- **Impacto:** la contraseña de 12 caracteres generada por el sistema es la credencial **definitiva y perpetua** del inquilino. Queda en el WhatsApp del administrador, en su portapapeles y en la memoria de su navegador. Nadie puede rotarla: ni el inquilino (no hay endpoint) ni el admin (re-invitar falla en `InviteTenantCommandHandler.cs:28-29` porque `tenant.UserId.HasValue`). Ante una filtración, la única mitigación es tocar la base a mano.
- **Remediación:** `POST /auth/change-password` + flag `MustChangePassword` forzado en el primer login; que la invitación entregue un token de un solo uso con expiración; permitir re-invitar; revocar refresh tokens al cambiar la contraseña.

### 🟠 ALTO — Dar de baja a un inquilino no revoca su acceso al portal

- **Ubicación:** `DeleteAppTenantCommandHandler.cs:18` · `UpdateAppTenantCommandHandler.cs:27`
- **Evidencia:** ambos hacen `tenant.IsActive = false/request.IsActive` y guardan. Ninguno toca el `User` vinculado por `tenant.UserId`, ni pone `User.IsActive = false`, ni revoca refresh tokens. `TenantLoginCommandHandler.cs:43` solo valida `user.IsActive`, nunca `appTenant.IsActive`.
- **Impacto:** un ex-inquilino dado de baja **sigue autenticándose indefinidamente**. Mientras el contrato siga `Active`, los cuatro handlers de `/me` le devuelven contrato, transacciones, historial y documentos compartidos, y puede seguir emitiendo tokens de descarga. Es una revocación que la UI promete y el backend no cumple.

### 🟠 ALTO — Token de acceso de 8 horas sin revocación: el logout es cosmético

- **Ubicación:** `JwtService.cs:35-40` · `appsettings.json:10` · `AuthController.cs:80-105`
- **Evidencia:** `expires: DateTime.UtcNow.AddHours(_settings.ExpiryHours)` con `ExpiryHours: 8`. El logout solo borra la cookie; el JWT también viaja en el body (`AuthResponseDto.cs:4`) y `Program.cs:126-131` acepta `Authorization: Bearer` con prioridad. No hay denylist de `jti` ni claim de versión de sesión.
- **Impacto:** quien capturó un JWT mantiene acceso completo hasta 8 horas, sin importar cuántas veces el usuario cierre sesión o sea desactivado — `user.IsActive` solo se evalúa en login y refresh, nunca en peticiones normales. La revocación en cascada de refresh tokens, que está bien implementada, queda neutralizada por el access token vigente.
- **Causa raíz:** el `ExpiryHours` está en 8 porque **ningún cliente usa el refresh** (ver §4). Resolver eso habilita bajarlo a 15-30 min.

### 🟡 MEDIO — `Organization.IsActive` nunca se lee: no se puede suspender una inmobiliaria

- **Ubicación:** `LoginCommandHandler.cs:29-34` · `TenantLoginCommandHandler.cs:28-33` · `RefreshAccessTokenCommandHandler.cs:66-67`
- **Evidencia:** el campo existe (`Domain/Entities/Organization.cs:9`) y se setea al crear, pero un grep sobre toda la solución muestra que **jamás se evalúa**. Un tenant moroso, offboardeado o comprometido sigue operando con normalidad.

### 🟡 MEDIO — Rate limiting inefectivo detrás de proxy, y apagado en Development

- **Ubicación:** `Program.cs:145-148` · `Program.cs:223-224`
- **Evidencia:** particiona por `httpContext.Connection.RemoteIpAddress`, y no hay `UseForwardedHeaders` en ningún archivo. Además `if (!app.Environment.IsDevelopment()) app.UseRateLimiter();`
- **Impacto:** detrás de un proxy o CDN, `RemoteIpAddress` es siempre la IP del proxy: los 20 req/min pasan a ser un **cupo global de toda la plataforma**. Un atacante monopoliza la ventana y deja a todos sin poder loguearse (DoS trivial) mientras su propia fuerza bruta no queda aislada. Sin lockout por cuenta, el password spraying queda dentro de cualquier límite por IP. **Especialmente débil contra móviles**, donde miles de usuarios comparten la IP del CGNAT del operador.

### 🟡 MEDIO — Cookies sin flag `Secure` detrás de un proxy que termina TLS

- **Ubicación:** `AuthController.cs:120-127` y `:134-143`
- **Evidencia:** `Secure = Request.IsHttps`. No hay `UseForwardedHeaders`, `UseHttpsRedirection` ni `UseHsts` en `Program.cs`.
- **Impacto:** en el patrón habitual (nginx/ALB termina TLS y habla HTTP con Kestrel), `Request.IsHttps` es `false` y tanto el access como el refresh token se emiten **sin** `Secure`.

### 🟡 MEDIO — CORS con credenciales cae a localhost si falta la configuración

- **Ubicación:** `Program.cs:45-53`
- **Evidencia:** el fallback `?? new[] { "http://localhost:5173", ... }` aplica en **cualquier** entorno, `appsettings.json` no tiene sección `Cors`, y `SecuritySettingsValidator` no valida esa clave.
- **Impacto:** si en producción se olvida `Cors__AllowedOrigins`, la API arranca sin error y acepta peticiones con credenciales desde localhost. **Falla en abierto, no ruidosamente.**

### 🟡 MEDIO — `AllowedHosts: "*"` + URL absoluta armada con el header `Host`

- **Ubicación:** `appsettings.json:23` · `DocumentsController.cs:64-66`
- **Evidencia:** `var absoluteUrl = $"{Request.Scheme}://{Request.Host}{result.Url}";` con el host filtering desactivado.
- **Impacto:** la respuesta pasa a ser `https://evil.com/api/v1/files/download?token=<HMAC válido>`. Con un CDN compartido delante, la respuesta envenenada se sirve a otros usuarios, cuyos clientes mandan el token al host del atacante.

### 🟡 MEDIO — Política de contraseñas solo por longitud, y sin anti-CSRF

- **Contraseñas** (`RegisterOrgCommandValidator.cs:14`): `MinimumLength(12)` sin complejidad ni lista de claves comunes — `"111111111111"` es válida para el Admin de una organización entera. El work factor de BCrypt no está fijado explícitamente (`RegisterOrgCommandHandler.cs:46`), así que no puede subirse por configuración.
- **CSRF** (`AuthController.cs:124` · `Program.cs:126-131`): la única defensa es `SameSite=Lax`. El propio comentario del código instruye migrar a `SameSite=None` en despliegues cross-site — que es exactamente lo que pasa al separar `api.x.com` de `app.x.com`. En ese momento todos los endpoints mutantes quedan expuestos sin ninguna capa restante.

### 🟡 MEDIO — Alta de organizaciones abierta y sin verificación de email

- **Ubicación:** `AuthController.cs:13-14`, `:25-34` · `RegisterOrgCommandHandler.cs:42-51`
- **Impacto:** cualquiera crea organizaciones ilimitadas (20/min por IP) con emails que no controla y obtiene un JWT de Admin al instante. Permite ocupar slugs de marcas reales de forma **irrecuperable** (`SlugExistsAsync` los bloquea para siempre). En una plataforma que maneja DNI y contratos, los tenants anónimos ilimitados además complican cualquier obligación como responsable de tratamiento.

### 🔵 BAJO — Otros

- **`ExistsForPeriodAsync` ignora el filtro global sin acotar por org** (`RentHistoryRepository.cs:70-73`): único método que rompe el patrón `*RawAsync(id, organizationId)` que el resto respeta. Impacto limitado hoy porque el `contractId` ya viene validado, pero es un oráculo booleano cross-tenant latente.
- **El token de descarga no revalida visibilidad ni usuario al canjearse** (`DocumentTokenService.cs:22-23` · `FilesController.cs:33-37`): si un admin revoca la visibilidad, el inquilino sigue descargando durante los 5 minutos restantes.
- **Cambiar el email de un inquilino no cambia su email de login** (`UpdateAppTenantCommandHandler.cs:25`): la UI muestra el nuevo, el login exige el viejo. Si esa dirección ya no le pertenece, el identificador queda en manos de un tercero.
- **Credenciales de dev versionadas** (`docker-compose.yml:8,25`): `devpassword` y `minioadmin/minioadmin`, con puertos sin bindear a loopback. Mitigado por `SecuritySettingsValidator.cs:75-78`, que las rechaza fuera de Development.
- **`TenantMiddleware` registrado antes de `UseAuthentication`** (`Program.cs:225-227`): hoy es un no-op, pero cualquier implementación futura leería un principal anónimo, y una resolución de tenant que falle a `Guid.Empty` desactiva el filtro global de EF Core.

### ✅ Controles correctos verificados

**Verificado en la auditoría dedicada de seguridad:**
- **Ningún Command o Query acepta `OrganizationId` del body o la query.** Los cinco commands que lo llevan se construyen en el controller desde `BaseController.OrganizationId`, leído del claim `org_id` (`BaseController.cs:18-20`). Ninguno de los 14 request DTOs de `API/Contracts/` contiene el campo. **La regla dura del proyecto se cumple.**
- Los 16 controllers revisados uno por uno: todos los de gestión heredan `AdminControllerBase` con `[Authorize(Roles = "Admin,Staff")]`. Ninguno queda con `[Authorize]` a secas.
- `GetDocumentDownloadUrlQueryHandler` implementa la cadena correcta: filtro de org (`:30`), coincidencia documento↔contrato contra enumeración (`:35`), y para no-staff exige `IsVisibleToTenant` (`:42`) **más** pertenencia del contrato al inquilino (`:44-48`).
- `MeController` resuelve el inquilino solo desde el claim `sub`, nunca desde parámetros del cliente.
- JWT: `ValidateIssuer/Audience/Lifetime/IssuerSigningKey` activados (`Program.cs:109-113`), `ClockSkew` bajado a 30 s, algoritmo `HmacSha256` fijo sin ruta a `alg: none`, longitud de clave verificada en dos capas, y fail-fast que rechaza placeholders `REPLACE_WITH` en cualquier entorno (`SecuritySettingsValidator.cs:38-43`).
- **Ecualización de tiempo con `DummyHash`** en las ramas org-inexistente y user-inexistente (`LoginCommandHandler.cs:18,32,39`): cierra la enumeración de cuentas por side-channel temporal.
- Separación estricta de portales: `login` rechaza `Role == Tenant` y `tenant-login` rechaza todo lo que no lo sea.
- Solo se persiste el SHA-256 del refresh token, con 256 bits de entropía vía `RandomNumberGenerator` (`RefreshTokenService.cs:22,26-31`).
- **Cero `FromSqlRaw`/`ExecuteSqlRaw`** en toda la solución; las dos llamadas a `migrationBuilder.Sql` son estáticas sin interpolación.
- **Inyección de fórmulas CSV mitigada** correctamente: prefijo de comilla simple ante `= + - @ TAB CR` (`API/Common/Csv.cs:8,17-19`).
- **Cero `dangerouslySetInnerHTML`, `innerHTML`, `eval` o `document.write`** en todo `web/src`. El JWT **no** se guarda en `localStorage`: el store persiste solo el perfil no sensible (`authStore.ts:26-28`).
- Headers: `nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer` (`Program.cs:201-203`), CSP `default-src 'none'` y HSTS de 1 año fuera de Development.
- `LoggingBehavior` loguea solo el nombre del tipo de request, nunca el objeto — `LoginCommand.Password` jamás llega a los logs (`Common/Behaviors/LoggingBehavior.cs:17-20`).
- `git log --all` sobre `appsettings.Development.json`: **nunca estuvo en el repositorio**. Los únicos archivos de configuración versionados tienen todos los campos sensibles en `""`.

**Verificado en la pasada de infraestructura:**
- Filtro global multi-tenant en las 8 entidades que corresponden (`AppDbContext.cs:35-57`), con las 3 excepciones documentadas y justificadas: `Organization` (raíz), `IndexValue` (global), `RefreshToken` (deliberado, `:62-63`).
- Los `IgnoreQueryFilters()` están acotados a repositorios de job, con el `OrganizationId` propagado explícitamente y re-verificado en el handler (`ApplyRentAdjustmentCommandHandler.cs:42-56`, `ContractRepository.cs:55-61`).
- Rate limiting en auth: 20 req/min por IP, sin cola (`Program.cs:143-156`, `AuthController` con `[EnableRateLimiting("auth")]`).
- Refresh tokens hasheados, con revocación, rotación y limpieza diaria (`RefreshTokenConfiguration.cs:24`, `RefreshTokenCleanupJob.cs:30-31`); el refresh token va en cookie HttpOnly, nunca en el body (`AuthController.cs:77`).
- Documentos privados por defecto (`documentService.ts:11`) con toggle explícito.

---

## 4. Correctitud y lógica de negocio

Los hallazgos §4.1 a §4.9 salieron de la auditoría de cobertura de tests; §4.10 en adelante, de la revisión de código dedicada.

### 🔴 CRÍTICO — División por cero en el cálculo de ajuste indexado

- **Ubicación:** `api/src/GestionAlquileres.Application/Features/RentHistory/Commands/ApplyRentAdjustmentCommandHandler.cs:125-126`
- **Evidencia:**
```csharp
newRent = Math.Round(previousRent * currentIndex.Value / baseIndex.Value, 2);
factor  = Math.Round(currentIndex.Value / baseIndex.Value, 6);
```
- **Escenario de falla:** la guarda de `:116-118` solo valida `baseIndex is null`, nunca `baseIndex.Value == 0`. Un `IndexValue` con valor 0 es perfectamente persistible: la entidad no valida rango y `SyncIndexCommandHandler.cs:98` copia el valor externo tal cual. Resultado: `DivideByZeroException` → 500 no controlado en vez del 409 de negocio.

### 🟠 ALTO — La fecha efectiva por defecto usa UTC, contradiciendo el propio job que la llama

- **Ubicación:** `ApplyRentAdjustmentCommandHandler.cs:64`
- **Evidencia:** `var effectiveDate = request.EffectiveDate ?? DateOnly.FromDateTime(DateTime.UtcNow);`
- **Escenario de falla:** entre las 21:00 y las 24:00 hora argentina, un `POST /adjust` sin `effectiveDate` se registra con la fecha del día siguiente. En el borde de fin de mes eso desplaza `periodT` (`:109`) al mes siguiente, y por lo tanto **selecciona el índice equivocado**. El helper `ArgentinaTime` existe y se usa en otros 5 lugares (`MonthlyRentAdjustmentJob.cs:34`, `SyncIndexesJob.cs:28`, `ContractExpiryNotificationJob.cs:28`, `GetDashboardQueryHandler.cs:23`, `RegisterPaymentCommandHandler.cs:76`) — este handler es el que quedó afuera.

### 🟠 ALTO — Deriva de cadencia en contratos que arrancan el 29, 30 o 31

- **Ubicación:** `api/src/GestionAlquileres.API/Jobs/MonthlyRentAdjustmentJob.cs:87-88`
- **Evidencia:**
```csharp
var baseDate = lastAdj?.EffectiveDate ?? contract.StartDate;
var nextAdj  = baseDate.AddMonths(frequencyMonths);
```
- **Escenario de falla:** con `StartDate = 2026-01-31` y frecuencia mensual, `AddMonths` recorta a `2026-02-28`; la corrida siguiente toma **esa** fecha como base (`:87`) y produce `2026-03-28`. La cadencia se corrió y nunca vuelve al 31. El comentario de `:92-94` afirma que queda anclada a `StartDate + k·frecuencia`, lo cual es falso justamente para los días 29-31.

### 🟠 ALTO — `MonthlyRentAdjustmentJob` no tiene un solo test

- **Ubicación:** `api/src/GestionAlquileres.API/Jobs/MonthlyRentAdjustmentJob.cs:32`, `:72`, `:90`, `:95`
- **Evidencia:** la única referencia al tipo fuera de su propio archivo es el registro en `Program.cs:243`. Ningún archivo bajo `api/tests/` lo menciona. Los tests de Phase9 ejercitan `ListActiveRawAsync` y `GetLastByContractRawAsync` **directamente contra los repositorios** (`Phase9Tests.cs:74`, `:92`) — verifican los ladrillos, nunca la pared.
- **Sin cubrir:** cálculo de `frequencyMonths`, anclaje a fecha programada, filtro `AdjustmentType != Manual`, aislamiento de scope por contrato, y que un contrato que falla no bloquee al siguiente.

### 🟠 ALTO — IPC nunca se ejercita en el cálculo, y el switch de frecuencia está muerto en tests

- **Ubicación:** `ApplyRentAdjustmentCommandHandler.cs:90`, `:101-107`
- **Evidencia:** ningún test aplica un ajuste sobre un contrato IPC. Las tres apariciones de `"IPC"` en los tests de contratos (`Phase4/ContractsControllerTests.cs:181`, `:287`, `:313`) solo **crean** contratos, nunca llaman a `/adjust`. Las cuatro ramas del switch de frecuencia (Monthly=1, Quarterly=3, Annual=12, default=3) tienen cobertura cero.
- **Consecuencia:** la propiedad más delicada del diseño —que para ICL el lookback es 12 meses **independientemente** de la frecuencia (`:92-98`)— solo se probó con Quarterly. Un cambio a "frecuencia = lookback" no rompería ninguna aserción.

### 🟠 ALTO — La idempotencia nunca se ejercita

- **Ubicación:** `ApplyRentAdjustmentCommandHandler.cs:69-71`
- **Evidencia:** los 12 call-sites del endpoint `/adjust` en la suite son invocaciones únicas por contrato. La guarda `ExistsForPeriodAsync`, que el comentario de `:66-68` describe como la protección central contra reintentos de Hangfire, **nunca se dispara en los tests**.
- **Agravante:** los tests usan `UseInMemoryDatabase` (`Phase5ApiFactory.cs:41`), que **no aplica índices únicos**. Aunque se escribiera el test, solo validaría la guarda aplicativa, nunca la de base de datos.

### 🟡 MEDIO — `newRent` puede quedar en 0 y romper el siguiente ajuste manual

- **Ubicación:** `ApplyRentAdjustmentCommandHandler.cs:125` → `:131` → `:86`
- **Evidencia:** el camino indexado no valida `newRent > 0` (el manual sí lo hace, en `:80`). Con datos de índice corruptos, `Math.Round(..., 2)` puede dar `0.00`, que se persiste en `contract.MonthlyRent`; el siguiente ajuste manual divide por ese cero en `:86`.

### 🟡 MEDIO — Redondeo bancario sin decidir, en importes en pesos

- **Ubicación:** `ApplyRentAdjustmentCommandHandler.cs:125`
- **Evidencia:** `Math.Round(x, 2)` sin `MidpointRounding` explícito usa banker's rounding (2,225 → 2,22). El único test de precisión (`Phase5/IclAdjustmentPrecisionTests.cs:107`) usa un caso cuyo tercer decimal es 8, o sea que redondea igual con cualquier modo — la decisión quedó sin cubrir a pesar de que el archivo se llama "PrecisionTests".

### 🟡 MEDIO — Aserciones que no pinchan nada

| Ubicación | Problema |
|---|---|
| `Phase2/Application/SyncIndexCommandHandlerTests.cs:216` | `Assert.NotNull(ex.Message)` — nunca es null en .NET |
| `Phase10/Phase10Tests.cs:81-82` y `:115-116` | `Assert.True(x == Conflict \|\| x == BadRequest)` — acepta dos comportamientos contradictorios, no fija contrato |
| `Common/ArgentinaTimeTests.cs:20` | Reproduce literalmente la implementación de `ArgentinaTime.cs:17`; pasa por construcción |
| `Phase2/Domain/IndexValueTests.cs:31-50` | Tres tests por reflexión que verifican tipos: si cambiaran, la suite no compilaría antes de ejecutarlos |
| `Phase4/GetAdjustmentProjectionQueryTests.cs:77-80` | Asevera sobre campos que el propio stub grabó; el `150_000m` del resultado no se verifica en ninguna línea |

### ✅ CORREGIDO (2026-07-31) — Autorización aplicada DESPUÉS de escribir: fuga de documentos entre contratos

> **Resuelto.** `SetDocumentVisibilityCommand` ahora lleva `ContractId` y el handler valida la pertenencia **antes** de mutar, devolviendo `null` (→ 404) si no coincide. De paso, un `docId` inexistente ya no produce un 500: `KeyNotFoundException` no estaba mapeado en `ExceptionMiddleware` y caía al catch genérico.
> Cobertura: `Phase7/DocumentsControllerTests.cs` T11 (regresión), T12 (el par correcto sigue funcionando) y T13 (documento inexistente → 404). **T11 se verificó reproduciendo la forma exacta del bug**: con el código original el 404 pasa y falla la aserción de que el documento sigue privado.



- **Ubicación:** `api/src/GestionAlquileres.API/Controllers/DocumentsController.cs:52-53`
- **Evidencia:**
```csharp
var dto = await Mediator.Send(new SetDocumentVisibilityCommand(docId, body.IsVisibleToTenant), ct);
return dto.ContractId != contractId ? NotFound() : Ok(dto);
```
El handler (`SetDocumentVisibilityCommandHandler.cs:18-19`) hace `doc.IsVisibleToTenant = req.IsVisibleToTenant` y `SaveChangesAsync` **antes** de que el controller compruebe la pertenencia al contrato.
- **Escenario de falla:** un Staff llama `PATCH /api/v1/contracts/{contratoA}/documents/{docDeContratoB}/visibility` con `{"isVisibleToTenant": true}`. El flag queda persistido en el documento del contrato B y recién entonces se devuelve `404`. El operador ve "no encontrado" y asume que no pasó nada, pero **el inquilino del contrato B ya puede descargar ese documento** por `GET /api/v1/me/documents` (`GetMyDocumentsQueryHandler.cs:35` filtra justamente por `IsVisibleToTenant`). El mismo vector sirve para ocultar documentos ajenos en silencio.
- **Corrección:** pasar `ContractId` dentro del comando y validar la pertenencia en el handler **antes** de mutar. El controller nunca debe ser el único punto donde se decide una autorización sobre un recurso ya escrito.

### 🟠 ALTO — El cliente nunca llama a `/auth/refresh`: la sesión muere de golpe

- **Ubicación:** `web/src/shared/lib/api.ts:17-24`
- **Evidencia:** un grep de "refresh" sobre todo `web/src` no devuelve **ninguna** coincidencia. El endpoint con toda su rotación y detección de reuso (`RefreshAccessTokenCommandHandler.cs:48-84`) está implementado en el backend y no lo invoca nadie.
- **Escenario de falla:** al cumplirse las 8 horas, la siguiente request devuelve 401 y el interceptor expulsa al login perdiendo el formulario en curso, pese a que la cookie de refresh sigue válida por 14 días. Es la causa raíz del hallazgo de seguridad del token de 8 h: no se puede acortar sin degradar aún más la experiencia.

### 🟠 ALTO — Enums de contrato sin `IsInEnum()`: el ajuste usa el índice equivocado

- **Ubicación:** `CreateContractCommandValidator.cs:9-16` · `UpdateContractCommandValidator.cs:9-16`
- **Evidencia:** ninguno valida `Currency`, `AdjustmentType` ni `AdjustmentFrequency`. Compárese con `CreatePropertyCommandValidator.cs:13`, que sí usa `IsInEnum()`.
- **Escenario de falla:** `POST /api/v1/contracts` con `"adjustmentType": 99` se persiste como `99` (esos enums se guardan como integer crudo). Luego, en `ApplyRentAdjustmentCommandHandler.cs:90`, el ternario `== AdjustmentType.ICL ? ICL : IPC` lo trata como **IPC**: el contrato se ajusta con el índice equivocado y ese importe queda como base del siguiente ajuste. Con `adjustmentFrequency: 99` cae en el `_ => 3` silencioso de la línea 106 y aplica un lookback trimestral no pedido.

### 🟠 ALTO — `UpdateContract` no revalida que propiedad e inquilino sean de la organización

- **Ubicación:** `UpdateContractCommandHandler.cs:22-23`
- **Evidencia:** asigna `contract.PropertyId` y `contract.AppTenantId` sin ninguna comprobación. El handler de creación **sí** la hace (`CreateContractCommandHandler.cs:26-30`).
- **Escenario de falla:** un `PUT` con el `propertyId` de **otra organización** satisface la FK (que es global) y se escribe. A partir de ahí el contrato queda corrupto en silencio: `Include(c => c.Property)` devuelve `null` por el filtro global, y —peor— los *inner joins* de los listados org-wide (`TransactionRepository.cs:53`, `RentHistoryRepository.cs:39`) **descartan todas las filas de ese contrato**: sus transacciones y ajustes desaparecen de las pantallas de Pagos y Ajustes sin ningún error. El mismo `PUT` tampoco impide editar un contrato ya `Terminated`.

### 🟠 ALTO — Exports CSV truncados a 500 filas, en silencio

- **Ubicación:** `TransactionRepository.cs:38-43` · `RentHistoryRepository.cs:24-29` · `DocumentRepository.cs:18-22`
- **Evidencia:** `.Take(500)` dentro de `GetAllAsync`, consumido por `GET /api/v1/transactions/export` y `/rent-adjustments/export`.
- **Escenario de falla:** una inmobiliaria con 80 contratos genera ~960 cargos al año más pagos. El contador exporta `transacciones.csv` para conciliar el ejercicio y recibe **solo las 500 más recientes**, sin advertencia, sin fila de corte y con HTTP 200. La conciliación sale mal y el error es indetectable desde el archivo.

### 🟠 ALTO — Cero `AsNoTracking()` en todo el backend, y el dashboard trae la cartera completa

- **Ubicación:** `ContractRepository.cs:22-31` y repos afines · `GetDashboardQueryHandler.cs:22,31,34`
- **Evidencia:** `grep AsNoTracking api/src/` no devuelve ninguna coincidencia en código fuente. El dashboard hace `ListAsync(null, null, Active, ct)` —que fuerza `Include(Property)` e `Include(AppTenant)`— para calcular tres escalares.
- **Escenario de falla:** con 5.000 contratos activos, cada carga del dashboard materializa 15.000 entidades trackeadas para producir tres números y una lista de 5 transacciones. Con `staleTime: 30_000` (`queryClient.ts:6`), se repite cada 30 s por cada admin conectado.

### 🟠 ALTO — N+1 en la liquidación al propietario, con filtrado de período en memoria

- **Ubicación:** `GetOwnerSettlementQueryHandler.cs:39-54`
- **Evidencia:** dos `foreach` anidados con una query por propiedad y otra por contrato; `GetByContractAsync` (`TransactionRepository.cs:16-21`) no acepta rango de período ni tiene `Take`, así que el filtro por fechas se hace en memoria.
- **Escenario de falla:** propietario con 40 propiedades y 5 años de historial → **81 round-trips a Postgres** y ~6.000 entidades trackeadas para sumar 40 filas, aunque el período pedido sea un solo mes. El tiempo crece con la antigüedad de la cartera, no con el rango consultado.

### 🟡 MEDIO — Los cuatro handlers de `/me` resuelven el contrato activo de forma ambigua

- **Ubicación:** `GetMyContractQueryHandler.cs:25-26` · `GetMyTransactionsQueryHandler.cs:30-31` · `GetMyRentHistoryQueryHandler.cs:30-31` · `GetMyDocumentsQueryHandler.cs:29-30`
- **Evidencia:** el mismo bloque copiado cuatro veces: `ListAsync(appTenant.Id, null, Active, ct)` seguido de `.FirstOrDefault()`, sobre un repositorio que ordena por `CreatedAt DESC`.
- **Escenario de falla:** un inquilino con dos contratos activos simultáneos (renovación cargada antes de rescindir la anterior, caso real) ve los pagos, documentos e historial del contrato **creado más recientemente**, sin ningún indicador de que existe otro. Y como cada handler resuelve por su cuenta, un cambio de orden en el repositorio los desincroniza entre sí. Además `/me/transactions` no pagina: 5 años son ~250 transacciones en una sola respuesta al teléfono.
- **Relevancia:** `/me/*` es exactamente la superficie de la v1 de la app mobile.

### 🟡 MEDIO — Otros de correctitud

- **Borrado de documento: el blob se elimina antes de confirmar la base** (`DeleteDocumentCommandHandler.cs:23-25`). Si `SaveChangesAsync` falla, la fila sobrevive apuntando a un `StorageKey` inexistente y toda descarga revienta con 500. `grep BeginTransaction|IUnitOfWork` sobre `api/src` confirma que **no hay ninguna transacción explícita en el proyecto**.
- **`ExceptionMiddleware` no verifica `Response.HasStarted`** (`ExceptionMiddleware.cs:29,39,49,56,68,78`). Si se corta el streaming de un PDF de 30 MB, fijar el status code lanza `InvalidOperationException` que enmascara el error original.
- **Invalidaciones de caché faltantes tras mutaciones** (`useContracts.ts:82-85,94-96,105-107` · `useDocuments.ts:16,24`). Registrar un pago no invalida `['transactions']`, `['dashboard']` ni `['me','transactions']`: la pantalla de Pagos y el **balance neto** muestran datos previos durante 30 s. `useUploadDocument` invalida `['documents', contractId]`, que **no** es prefijo de `['documents','all',…]`, así que la pantalla de Documentos no refleja altas ni bajas.
- **`Notes` sin `MaximumLength` en cuatro validadores** contra `HasMaxLength(2000)` en la base → Postgres devuelve `22001`, que el middleware no mapea (solo intercepta `23505`) → **HTTP 500** en vez de 400.
- **Búsqueda org-wide no sargable** (`TransactionRepository.cs:59-73`): `lower(...) LIKE '%texto%'` sobre expresiones concatenadas, sin índice que pueda servirlo. Cada tecleo dispara **tres** sequential scans del join completo (filas, `CountAsync` y `SumAsync` del balance).
- **Lookback por frecuencia duplicado y ya divergente** (`ApplyRentAdjustmentCommandHandler.cs:99-107` vs `GetAdjustmentProjectionQuery.cs:43-50`): para un contrato ICL trimestral, la pantalla de proyección calcula con ventana de 3 meses y el ajuste real usa 12. El operador le comunica al inquilino un importe futuro que después no coincide con el cargo.
- **Mapeo entidad→DTO duplicado ×6** y handlers de Query dependiendo de handlers de Command (`ListContractsQueryHandler.cs:17` usa `CreateContractCommandHandler.ToDto`). `AutoMapper` está declarado y registrado pero **no se usa en ninguna parte**.
- **`GetExpiringRawAsync` usa UTC** en vez de `ArgentinaTime` (`ContractRepository.cs:65-66`): única fuga de UTC que queda en un repositorio, y sesga en un día el borde de la ventana de vencimientos.
- **Retry de React Query por regex sobre `error.message`** (`queryClient.ts:8-11`): funciona solo porque axios formatea el mensaje de cierta manera; un 409 o un 400 se reintentan dos veces más.

### ✅ Lo que está bien

**De la revisión de código dedicada:**
- **La frontera de Clean Architecture es impecable.** `Domain.csproj` no tiene una sola `PackageReference`; `Application.csproj` solo referencia Domain + MediatR/FluentValidation. El grep de `DbContext|HttpClient|EntityFrameworkCore` sobre Application encuentra únicamente un comentario. **Cero violaciones reales.**
- **Controllers verdaderamente delgados:** los 16 suman 749 líneas y ninguno contiene lógica de negocio.
- Concurrencia optimista bien razonada y documentada (`ContractConfiguration.cs:28-34`), traducida a 409 por el middleware.
- Generación de contraseñas temporales sin sesgo modular, con rejection sampling (`InviteTenantCommandHandler.cs:56-62`).
- Whitelist de MIME types que bloquea contenido activo (HTML/SVG/JS), con la razón explicitada (`UploadDocumentCommandValidator.cs:9-20`).
- Health checks separados liveness/readiness para no provocar restart loops ante un blip de DB (`Program.cs:268-279`).
- Densidad de comentarios explicativos ("por qué", no "qué") muy por encima de la media, con trazabilidad a auditorías previas.

**De la auditoría de tests:**
- `Phase5/IclAdjustmentPrecisionTests.cs` es un test modelo: aserción numérica exacta, comentario que documenta el bug que previene, y distingue correctamente `NewRent` de `AdjustmentFactor`.
- Idempotencia con doble guarda: chequeo aplicativo (`:69`) + índice único en base (`RentHistoryConfiguration.cs:25`).
- El job se ancla a la fecha **programada** y no a la de corrida (`MonthlyRentAdjustmentJob.cs:86-90`).
- `SyncIndexCommandHandler.cs:36-41` corta la llamada externa si el período ya está persistido.

---

## 5. Persistencia y esquema

**Panorama:** 12 migraciones, 11 tablas, 30 índices, 18 FKs, EF Core 8.0.11 con `UseSnakeCaseNamingConvention()`.

### ✅ Aislamiento multi-tenant a nivel índices: correcto

**No hay una sola tabla con `organization_id` que carezca de índice org-first.** Las 9 están cubiertas; la migración 12 (`20260718142104_AddScalabilityIndexesAndContractConcurrency.cs:26-34`) cerró los dos últimos huecos convirtiendo el índice de una columna en compuesto `(organization_id, <clave de orden>)` para `rent_history` y `transactions`.

Tipos de datos: **todo el dinero es `numeric(14,2)`**, todas las fechas puras son `date` (`DateOnly`), todos los timestamps son `timestamp with time zone` (`DateTimeOffset`). Sin `text`, sin `timestamp without time zone`. Esto está bien resuelto.

### 🟠 ALTO — Falta el único constraint que evitaría cargos duplicados

- **Ubicación:** `api/src/GestionAlquileres.Infrastructure/Persistence/Configurations/TransactionConfiguration.cs:22-29`
- **Evidencia:** `rent_history` tiene su guarda de idempotencia (`RentHistoryConfiguration.cs:25`, unique sobre `(ContractId, EffectiveDate)`), pero `transactions` **no tiene ningún índice único**.
- **Impacto:** hoy el `Transaction` de tipo `RentCharge` se crea en el mismo `SaveChangesAsync` que el `RentHistory` (`ApplyRentAdjustmentCommandHandler.cs:149-163`), así que el unique de `rent_history` lo cubre indirectamente. Pero **cualquier otro camino** que genere un cargo para el mismo contrato/período lo duplica sin que la base lo impida.
- **Remediación:** unique sobre `(organization_id, contract_id, period, type)`.

### 🟠 ALTO — Ninguna tabla tiene `updated_at` (bloqueante para sync mobile)

- **Evidencia:** un grep de `updated_at`/`UpdatedAt` sobre `api/src/**/*.cs` devuelve **cero resultados**. `created_at` sí está en las 11 tablas.
- **Impacto:** las filas mutables (`contracts.monthly_rent`, `properties.*`, `owners.*`, `app_tenants.*`, `users.*`) no tienen marca de última modificación: no hay watermark de sincronización incremental, no hay API "modified since", no hay rastro de auditoría sobre ediciones. **Para una app mobile con sincronización delta, esto es un prerequisito.** Tampoco hay `created_by`/`updated_by` en ninguna tabla — `documents.uploaded_by_user_id` es la única columna de actor del esquema.

### 🟡 MEDIO — Cero check constraints

- **Evidencia:** ni un `HasCheckConstraint` ni un `AddCheckConstraint` en todo el repo.
- **Sin proteger a nivel base:** `contracts.day_of_month` (nada lo acota a 1-31, `20260517040803_AddContracts.cs:28`), `end_date > start_date`, `transactions.amount > 0`, rango de `properties.commission_pct`.

### 🟡 MEDIO — Enums serializados de forma inconsistente entre tablas

- **Evidencia:** `contracts.currency`, `adjustment_type`, `adjustment_frequency` y `status` se guardan como `integer` crudo, mientras que `users.role` y `properties.property_type` se guardan como `varchar(20)` vía `HasConversion<string>()`.
- **Impacto:** reordenar un enum corrompe silenciosamente las columnas integer.

### 🟡 MEDIO — `documents.uploaded_by_user_id` no tiene foreign key

- **Ubicación:** `20260517154105_AddDocuments.cs:25`, `DocumentConfiguration.cs:20`
- **Evidencia:** declarado `uuid NOT NULL` con `.IsRequired()`, pero sin `HasOne<User>()` — la base no rechaza ids de usuario huérfanos o inventados.

### 🟡 MEDIO — Concurrencia optimista solo en `Contract`

- **Ubicación:** `ContractConfiguration.cs:34` — `builder.UseXminAsConcurrencyToken();`
- **Impacto:** `Transaction` tiene la misma exposición a lost updates (un pago marcando `status`/`paid_at` en paralelo con el job de generación de cargos) y no tiene token. Igual `Property.CommissionPct` y `Owner`.

### 🔵 BAJO — Cuatro índices redundantes

`ix_users_organization_id`, `ix_app_tenants_organization_id`, `ix_contracts_organization_id` e `ix_transactions_contract_id` son prefijos izquierdos estrictos de índices compuestos que ya existen. La migración 12 eliminó exactamente esta clase de redundancia para `rent_history`/`transactions` pero dejó estos cuatro.

### 🔵 BAJO — `ix_transactions_status_due_date` no es org-first

`20260622000654_AddTransactionStatus.cs:33-36` crea `(status, due_date)`. Como toda query pasa por el filtro de tenant, una consulta de morosidad es `WHERE organization_id = @org AND status = 0 AND due_date < @d`: Postgres tiene que escanear el índice a través de **todos los tenants** y filtrar después. En la tabla de mayor cardinalidad, es el único índice que no respeta el orden de aislamiento. La forma correcta sería `(organization_id, status, due_date)`.

---

## 6. Producto, UX y accesibilidad

Auditoría completa: **71 hallazgos, 20 de nivel alto**, sobre 78 archivos. Lo más relevante:

### 🔴 CRÍTICO — Todas las fechas se muestran un día antes

- **Ubicación:** `web/src/shared/lib/formatters.ts:6-7`
- **Evidencia verificada empíricamente** (TZ America/Argentina/Buenos_Aires): `formatDate('2026-01-01')` → **"31 de dic de 2025"**. `new Date('2026-01-01')` parsea el string como UTC medianoche; en UTC-3 eso es el día anterior a las 21:00.
- **Fricción:** un contrato que arranca el 1/1/2026 figura iniciado el 31/12/2025. El inquilino ve "vence 30 dic" cuando vence el 31. La fecha efectiva de cada ajuste aparece corrida. **En un producto cuyo valor es la trazabilidad legal de los aumentos, esto invalida la evidencia que la pantalla muestra.** Afecta al menos: `ContratoDetailPage.tsx:143,144,365,574`, `ContratoPage.tsx:81,133`, `HomePage.tsx:103,164`, `AjustesPage.tsx:126`, `PagosPage.tsx:149`, `DocumentosAdminPage.tsx:127`, `DocumentosPage.tsx:138`.
- **Relacionado:** `DashboardPage.tsx:159` y `ContratoDetailPage.tsx:255` formatean un período mensual con `formatDate` — `formatDate('2026-05')` → "30 de abr de 2026". El admin ve el mes equivocado en el panel principal.

### 🟠 ALTO — Flujos que la interfaz promete y no existen

| Flujo | Ubicación | Qué pasa |
|---|---|---|
| Editar contrato | `ContratoDetailPage.tsx:553-554` | Botón "Editar" sin `onClick`. El hook `useUpdateContract()` (`useContracts.ts:29`) **no se usa en ninguna parte**. Corregir un alquiler mal cargado obliga a rescindir y recrear |
| Filtros Estado/Índice | `ContratosPage.tsx:335-336` | Dos botones con chevron que no responden |
| Débito/crédito manual | `useContracts.ts:100-109` | `useRegisterManualCharge()` implementado y **sin un solo consumidor**. Los tipos aparecen en los filtros de ambos portales, pero no hay forma de crearlos — el caso "le descuento medio mes por la rotura del termotanque" (paso 8 del flujo de negocio de `CLAUDE.md`) no se puede ejecutar |
| Aplicar ajustes | `AjustesPage.tsx:44-145` | La pantalla "Ajustes" es solo historial. La tarea recurrente más importante ("es 1° de mes, ¿a quién le toca?") no tiene pantalla |
| Registrar pago desde Pagos | `PagosPage.tsx:63-86` | Solo "Exportar CSV". Cargar un pago son 5 pasos vía Contratos |
| Cerrar sesión (inquilino) | `InquilinoLayout.tsx:9-13` | Un grep de `logout` sobre todo `portal-inquilino/` devuelve **cero resultados** |
| Recuperar contraseña | `portal-inquilino/pages/LoginPage.tsx:93-95` | `<a>` sin `href` ni `onClick`: parece link, no navega, no recibe foco |

### 🟠 ALTO — El panel principal muestra tendencias inventadas junto a datos reales

- **Ubicación:** `web/src/portal-admin/pages/DashboardPage.tsx:34-35`
- **Evidencia:**
```ts
const REVENUE_SERIES = [4.2, 4.4, 4.5, 4.7, 4.9, 5.1, 5.3, 5.4, 5.7, 5.9, 6.2, activeContracts || 6.5]
```
- **Fricción:** cada tarjeta financiera muestra una curva ascendente fabricada, con el valor real solo en el último punto. El descargo dice "tendencia (ilustrativa)" en 11,5 px gris. El usuario mira "Ingresos mensuales" con una curva que sube y concluye que su cartera crece. **Es un riesgo de producto, no de UX: es una afirmación falsa sobre el negocio del cliente.**

### 🟠 ALTO — Al inquilino se le muestra la URL prefirmada cruda

- **Ubicación:** `web/src/portal-inquilino/pages/DocumentosPage.tsx:193-200`
- **Fricción:** toca un PDF y recibe un bloque monoespaciado de 200+ caracteres con el token, más un cronómetro. Es una interfaz de desarrollador en un teléfono. Y encima invita a copiar y reenviar el link por WhatsApp, que es exactamente lo que el diseño de URLs de corta duración busca evitar.

### 🟠 ALTO — Los seis campos de las operaciones financieras usan una clase CSS que no existe

- **Ubicación:** `ContratoDetailPage.tsx:214, 218, 222, 321, 325, 329`
- **Evidencia:** `className="inp"` — `grep "\.inp\b" index.css` no devuelve nada. Lo definido es `.input` (`index.css:219`).
- **Fricción:** registrar un pago y aplicar un ajuste manual —las dos operaciones que tocan plata— se renderizan con el estilo por defecto del navegador: sin borde del sistema, sin estado de foco, desalineados. Arreglo de 6 caracteres.

### 🟠 ALTO — Contraste por debajo de WCAG AA en los chips de estado

- **Ubicación:** `web/src/index.css:244-249`, tamaño en `:239` (11,5 px → requiere 4.5:1)

| Clase | Ratio | AA |
|---|---|---|
| `.chip--warn` / `.chip--ipc` | 3.54:1 | ✗ |
| `.chip--ok` | 3.82:1 | ✗ |
| `.chip--info` | 4.38:1 | ✗ |
| `.chip--danger` | 4.48:1 | ✗ |
| `.chip--icl` | 5.66:1 | ✓ |

Los chips son el vehículo principal de estado en todo el producto (Vigente/Vencido/Rescindido, Pago/Cargo, ICL/IPC).

### 🟠 ALTO — Accesibilidad estructural

- **~28 campos de formulario sin `<label>` asociado**: `ContratosPage.tsx:186,200,216,225,236,247,258,270,282,294,306`, `PropiedadesPage.tsx:145-188`, `InquilinosPage.tsx:173-203`, `ContratoDetailPage.tsx:213-328`. Un grep de `htmlFor` sobre todo `src` devuelve exactamente **9 ocurrencias**, todas en auth e índices.
- **Cero navegación por teclado en filas clickeables**: grep de `onKeyDown|tabIndex` sobre `src` → **cero resultados**. `ContratosPage.tsx:387-391`, `DashboardPage.tsx:108`, `DocumentosPage.tsx:122-130`.
- **`lang="en"` y `<title>web-scratch</title>`** en `web/index.html:2` y `:7` — un lector de pantalla lee todo el español con fonética inglesa.
- **Ninguna región `aria-live`** en toda la app: ninguna acción de creación confirma éxito.
- **Modales sin Escape ni trampa de foco**: grep de `Escape` → cero resultados. La excepción bien hecha es `SyncIndexDialog.tsx:25,44-52`, que usa `<dialog>` nativo.

### 🟡 MEDIO — Terminología y microcopy

- "Slug" expuesto como campo de la primera pantalla del producto, con error "Slug inválido" (`LoginForm.tsx:7,37`).
- "Ajustes" en el menú (`AdminSidebar.tsx:14`) significa aumentos de alquiler, pero en español de software significa *settings* — y "Configuración" está en `:17`. Ambigüedad garantizada.
- Estados en inglés visibles al inquilino: solo se traduce `Active`; si el contrato está `Expired` o `Terminated`, el inquilino lee eso literal (`HomePage.tsx:96-98`, `ContratoPage.tsx:73-75`).
- Al inquilino se lo saluda con la parte local de su email: "Hola de nuevo / jperez84" (`HomePage.tsx:53`), teniendo `appTenantFullName` disponible.
- El balance neto del admin pierde el signo negativo: −$1.200.000 se muestra como "$ 1.200.000" en rojo (`PagosPage.tsx:157-159`). El portal del inquilino sí lo hace bien.

### ✅ Lo que está bien resuelto

`ConfirmDialog` con `role="alertdialog"` reemplazando `confirm()` nativo · `QueryError` con reintento y `role="alert"` · `SyncIndexDialog` con `<dialog>` nativo · skip link (`AdminLayout.tsx:12`) · `:focus-visible` bien aplicado (`index.css:202-216`) · la variación de un ajuste deliberadamente **no** se pinta de verde/rojo, con el razonamiento documentado (`index.css:348-350`) · sello de fuente de datos condicional ICL→BCRA / IPC→INDEC (`ContratoDetailPage.tsx:106-113`) · paginación server-side con `keepPreviousData` · CSV con BOM para que Excel lea UTF-8 (`exportCsv.ts:11`) · 404 consciente del rol (`NotFoundPage.tsx:9`).

---

## 7. Readiness para la app mobile

### 7.1 Backend

**Superficie de API — verificada completa.** Todas las rutas bajo `/api/v1/...` por convención de `[Route]`; **no hay librería de versionado** instalada, así que el versionado es puramente por string de ruta. Serialización: camelCase por defecto de `JsonSerializerDefaults.Web`; **todos los enums salen como string** con el nombre exacto del miembro C# (`"ICL"`, `"RentCharge"`, `"Active"`), nunca como número (`Program.cs:57-62`).

| Bloqueante | Ubicación | Por qué importa en mobile |
|---|---|---|
| **Sin `updated_at` en ninguna tabla** | §5 | Sin watermark no hay sync incremental ni "modified since". Es el prerequisito #1 |
| **Storage local por defecto** | §2 | Una app que sube fotos desde la cámara contra un storage efímero pierde los datos |
| **Sin presigned URLs reales** | grep `PreSigned` sobre `api/src` → **cero hits**; `FilesController.cs:37-38` | Todo byte de todo documento pasa por el proceso de la API. En redes móviles y con fotos, no escala |
| **Token de descarga en query string** | §3 | En mobile los links se comparten y se cachean todavía más |
| **Tres formas distintas de paginar** | `TransactionsPageDto` con `netBalance` · `PagedResult<T>` · arrays planos sin paginar | El cliente mobile necesita un contrato uniforme; hoy hay tres |
| **Endpoints sin paginar** | `/contracts`, `/properties`, `/tenants`, `/owners`, todos los `/me/*` | Payloads que crecen sin techo sobre red móvil |
| **Sin `totalPages` ni `hasNext`** | `PagedResult.cs:8` | Scroll infinito hay que derivarlo a mano |
| **Sin push notifications** | — | No hay infraestructura de notificaciones; la comunicación es solo email, y el proveedor por defecto es el no-op |
| **Refresh token en cookie HttpOnly** | `AuthController.cs:77` | Funciona para browser; un cliente nativo necesita el token en el body o un flujo alternativo |

**Nota de precisión para el cliente mobile:** ningún DTO usa `DateTime` — es `DateOnly` (`"yyyy-MM-dd"`) o `DateTimeOffset` (ISO-8601 con offset). Los campos `DateOnly` (`startDate`, `endDate`, `period`, `dueDate`, `effectiveDate`) parseados con `new Date(str)` se interpretan como UTC medianoche y **se corren un día en Argentina** — exactamente el bug que ya está en producción en el frontend web (§6). Cualquier cliente nuevo debe evitar repetirlo.

### 7.2 Frontend — qué se porta y qué no

**Lo que ya sirve:** el **portal del inquilino ya está diseñado mobile-first** y es el mejor activo del proyecto. Contenedor de ancho de teléfono en las cuatro pantallas (`HomePage.tsx:60`, `ContratoPage.tsx:69`, `PagosPage.tsx:75`, `DocumentosPage.tsx:99`, con `maxWidth: 420` y `padding-bottom: 80px` ya reservando la nav inferior), tabs inferiores fijos (`HomePage.tsx:23-29`), bottom sheet con grabber (`DocumentosPage.tsx:172-181`), listas en tarjetas apiladas.

**Lo que se rompe:**

- 🟠 **Las dos pantallas de login son inusables en un teléfono.** `portal-admin/pages/LoginPage.tsx:36-42` y `portal-inquilino/pages/LoginPage.tsx:34-40`: grid `'1fr 1.1fr'` fijo con `padding: '64px 88px'`, sin ninguna media query (las de `index.css:371-391` no alcanzan estilos inline). En 390 px de ancho, el formulario queda con ~10 px útiles.
- 🟠 **Todas las grillas de las pantallas clave son inline y no colapsan.** `DashboardPage.tsx:123`, `ContratoDetailPage.tsx:75`, `:89` (la fórmula del ajuste en 5 columnas), `:211`, `:318`, `ContratoPage.tsx:102`. La celda "Alquiler proyectado" —el corazón funcional— queda de ~60 px con un importe de 8 dígitos adentro.
- 🟠 **El sidebar admin colapsa a un riel de 9 íconos sin etiquetas ni drawer** (`index.css:378-386`); el propio código lo admite en `:369-370`. `IcDoc` se usa dos veces, para "Contratos" y para "Documentos".
- 🟡 Tablas de 7-8 columnas con scroll horizontal y `th` sticky pensado para scroll vertical: al desplazarse se pierde de vista qué inquilino es cada fila.
- 🟡 Targets táctiles bajo el mínimo AA de 24×24: `.btn--sm` 28 px (`index.css:199`), chips usados como tabs 22 px (`index.css:237`, `ContratosPage.tsx:347-354`).
- 🟡 Sin `inputMode` ni `autoComplete` en formularios de negocio: cargar un DNI abre el teclado alfabético.
- 🟡 **Cero soporte PWA**: sin manifest, sin `theme-color`, sin íconos por densidad.

### 7.3 MVP mobile sugerido por perfil

**Inquilino** (el caso más fuerte — el 100% del uso es telefónico):
1. **Informar pago con foto del comprobante.** No existe hoy; es lo único que convierte el portal en algo que se abre en vez de consultarse. La cámara es justamente lo que aporta un teléfono.
2. **Push del próximo ajuste** con el detalle "de $X a $Y desde el 15/07 por ICL". Es la promesa central del producto (`portal-inquilino/pages/LoginPage.tsx:203`) y todavía no está.
3. Recordatorio de vencimiento (`contract.dayOfMonth` ya está disponible).
4. Documentos con descarga directa, sin la pantalla de URL cruda.
5. Estado de cuenta — ya bien resuelto, solo falta paginarlo.

**Administrador/propietario** (uso ocasional, para consulta y aprobación):
1. Cobros del día y marcar recibido en dos toques.
2. Ajustes pendientes del período con aprobación en un toque — requiere construir primero la vista de pendientes, que no existe.
3. Búsqueda de contrato + ficha de solo lectura para atender una llamada fuera de la oficina.
4. Notificación de índice publicado.
5. **Deliberadamente NO en la v1:** alta de contrato (11 campos), alta de propiedad, alta de inquilino, configuración. Son tareas de escritorio.

### 7.4 Deuda a saldar ANTES de empezar la app

1. **`formatters.ts:6`** — si se porta el patrón, la app nativa muestra vencimientos corridos, y en mobile los recordatorios se disparan por fecha. Un bug de fecha en un push es mucho peor que en una tabla.
2. **Los flujos rotos o inexistentes** (editar contrato, débitos/créditos manuales, informar pago, ajustes pendientes). No tiene sentido diseñar pantallas mobile sobre flujos que no cierran en web, donde iterar es barato.
3. **`updated_at` en el esquema** — sin esto no hay sincronización delta posible.
4. **Storage persistente + presigned URLs** — antes de que la app empiece a subir fotos.
5. **Sacar los estilos inline a clases con tokens** — los inline no se portan a React Native ni responden a media queries; un sistema de tokens se traduce casi 1:1 a un theme nativo. Es el trabajo de mayor apalancamiento.
6. **Sistema de feedback (toast + `aria-live`)** — en mobile no hay hover: la confirmación explícita es la única señal de que la acción ocurrió.
7. **Decidir la estrategia técnica.** El portal del inquilino ya es mobile-first: convertirlo en PWA instalable (manifest + service worker + push) es cuestión de días contra meses de una app nativa. Lo que la PWA no resuelve bien en iOS es push — que es justamente la feature #2 de la lista del inquilino. **Esa es la disyuntiva a resolver antes de elegir stack.**

---

## 7bis. Mercado y estrategia

### 🔴 El motor de ajustes no cubre el contrato argentino típico de 2026

- **Evidencia:** `Domain/Enums/AdjustmentType.cs:3` → `{ ICL, IPC, Manual }` · `AdjustmentFrequency.cs:3` → `{ Monthly, Quarterly, Annual }` · `Contract.cs:13-16` → un `MonthlyRent` y un `AdjustmentType` únicos, sin cronograma de escalones ni tope/piso.
- **Estándar de mercado:** tras el DNU 70/2023 hay **libertad plena** para pactar cualquier índice y frecuencia ([decreto](https://www.argentina.gob.ar/normativa/nacional/decreto-70-2023-395521/texto)). En la práctica 2026 dominan **IPC cuatrimestral** y **% fijo escalonado**, y Casa Propia/ICP ajusta **semestral** ([La Nación, mayo 2026](https://www.lanacion.com.ar/propiedades/casas-y-departamentos/alquileres-de-cuanto-es-el-aumento-en-los-contratos-que-se-ajustan-en-mayo-de-2026-nid30042026/)). Ubiquo soporta ICL + IPC + Casa Propia + escalonados; Barreeo soporta IPC + ICL + **porcentaje fijo**.
- **Impacto comercial:** una inmobiliaria que evalúa el producto intenta cargar "8% trimestral" o "IPC cuatrimestral" en la demo y **no puede**. No es un feature faltante: es una barrera de entrada al mercado. Costo de cierre bajo, impacto máximo.

### 🔴 Cero capacidad de cobranza, en un mercado donde la mora es estructural

- **Evidencia:** `TransactionType.cs:3` → `{ RentCharge, Payment, ManualDebit, ManualCredit }` — sin medio de pago, sin referencia externa, sin imputación pago→cargo. No hay SDK de MercadoPago ni de ningún PSP en `Infrastructure.csproj:8-30`. `Owner.cs:15` guarda `Cbu` como texto: la transferencia al propietario es 100% manual. No hay punitorio, ni interés, ni plan de pago.
- **Contexto:** **73% de los hogares inquilinos acumula deudas** y 29,7% está en emergencia habitacional (Encuesta Nacional Inquilina, jun-2026). Barreeo vende explícitamente "punitorios automáticos calculados día a día".
- **Impacto:** el producto que gana no es el que calcula bien el ICL —eso ya lo hacen todos— sino el que **reduce días de mora**. Es además la única vía a un ingreso que escale más rápido que el número de clientes.

### 🟠 Otros gaps de paridad

| Gap | Evidencia | Qué hace el mercado |
|---|---|---|
| **Sin WhatsApp** | `appsettings.json:42` proveedor de email en no-op; sin dependencia de Twilio/Meta | Barreeo, TusAlquileres y CONSO lo tienen. ~USD 0,026 por conversación utility: recordar el vencimiento a 500 inquilinos cuesta menos de ARS 20.000/mes |
| **Sin recibos ni liquidaciones en PDF** | Ninguna librería de PDF en `Infrastructure.csproj:8-30`; el único export es CSV | Es el entregable físico del negocio. Sin esto la inmobiliaria sigue armando el recibo en Word |
| **Sin portal de propietario** | `Owner` no tiene relación con `User`: el propietario no puede loguearse | AsiProp ofrece vistas separadas por link firmado. El propietario es quien presiona a la inmobiliaria |
| **Liquidación incompleta** | `TransactionType` sin tipo gasto; `Property.CommissionPct` es un % simple sin honorario fijo ni mínimo | El estándar es "cobrado − comisión − expensas − reparaciones". Si no cierra sola, el admin vuelve a Excel |
| **Sin mantenimiento ni tickets** | No existe la entidad | Segundo motivo de apertura de una app de inquilino. Buildium hace triage con IA |
| **Sin facturación ARCA** | Sin WSFE/WSAA; `Owner.TaxId` no se usa para emitir nada | El RELI dejó de ser obligatorio ([RG 5545/2024](https://servicioscf.afip.gob.ar/publico/sitio/contenido/novedad/ver.aspx?id=4234)), pero la factura de comisión sigue siéndolo |
| **Sin IA** | Sin dependencias de OpenAI/Azure AI/OCR | Barreeo ya vende "cargá contratos con IA en segundos". Elimina el mayor costo de switching: migrar 200 contratos a mano |
| **`plan` no monetiza** | `Organization.cs:8` → `Plan = "free"` sin límites, sin contador, sin facturación | Todos los competidores tienen tiers por volumen de contratos |

### Competencia directa verificada

| Producto | Precio publicado | Diferenciales |
|---|---|---|
| [Barreeo](https://barreeo.com/) | ARS 29.900 (5 alq.) → 149.900 (ilimitado)/mes | % fijo, punitorios día a día, WhatsApp, ARCA, carga con IA, portal con logo |
| [AsiProp](https://asiproplabs.com/) (partner oficial de Tokko) | ARS 39.100 (1-50) → 150.000 (501-1000)/mes | Portal **por link sin usuario ni contraseña**, mantenimiento con timeline de fotos, recibos numerados |
| [Ubiquo](https://ubiquo.com.ar/) | A cotizar, −20% socios CIA | ICL + IPC + **Casa Propia** + escalonados. 220+ inmobiliarias en AR/CL/PE/SV |
| [TusAlquileres](https://tusalquileres.com.ar/) | A demo | USD con actualización diaria, WhatsApp masivo, **facturación AFIP a terceros** |

**Patrón de pricing argentino:** se cobra **por contrato administrado**, en tiers, en pesos con IVA incluido, **nunca por usuario**. Cobrar por usuario sería un error de posicionamiento local.

### Sobre la estrategia mobile — validación y objeción

**Validado:** una sola APK con branding en runtime es **exactamente** lo que hacen Buildium ([Resident Center](https://www.buildium.com/features/resident-center/), con "logo y colores de cada administradora" dentro de la app compartida) y AppFolio. Además es la única lectura de la guideline **4.2.6 de Apple** que no obliga a que cada inmobiliaria tenga cuenta de desarrollador — la guideline señala el binario único agregado como la alternativa válida a las apps por plantilla. Android primero también es correcto: **86,11%** del mercado móvil argentino ([StatCounter, jun-2026](https://gs.statcounter.com/os-market-share/mobile/argentina)).

**La objeción — de secuencia, no de arquitectura:**

1. **La v1 no le da al inquilino una razón para instalar.** El estándar de la categoría es *pagar el alquiler* y *abrir un ticket*. Una app que se abre una vez al mes para sacarle una foto a un comprobante **compite en desventaja con WhatsApp**, que el inquilino ya tiene instalado, donde ya está la conversación y donde no hay que recordar una contraseña.
2. **El comprador no es el usuario.** Paga la inmobiliaria; la app v1 es para el inquilino. Es la única inversión de la lista cuyo beneficiario directo no firma el cheque.
3. **Costo de oportunidad.** Los 12 bloqueantes + 5 fases son un trimestre largo cuyo output es un canal de distribución, no una capacidad de negocio. En el mismo esfuerzo entran WhatsApp + punitorios + PDF + % fijo escalonado: cuatro razones para que una inmobiliaria firme.

**Dato que corta en ambos sentidos:** ningún competidor argentino de alquileres declara app de inquilino. Puede ser un hueco de mercado real, o puede ser que el mercado ya midió el ROI.

**Métrica de decisión propuesta:** si el portal PWA de inquilino no supera 35-40% de MAU sobre inquilinos activos, la APK no va a mover esa aguja — el problema sería de valor, no de empaquetado.

### Riesgo estratégico a tener presente

Hubo intentos de rechazo del DNU y hay presión legislativa recurrente. **El motor de ajustes debe diseñarse como configuración de datos, no como enum en código**: un índice nuevo tiene que ser una fila en `index_values`, no un deploy. Hoy `AdjustmentType` e `IndexType` son enums compilados.

---

## 8. No verificado / pendiente

**Auditorías que no llegaron a correr:**
- **Seguridad dedicada** (Agente 1): quedaron sin revisar de forma sistemática la emisión y validación de JWT, políticas de authorization por endpoint, IDOR entre tenants, XSS en el frontend, CORS, política de contraseñas y hashing, y el barrido de secretos en el historial de git.
- **Bugs y correctitud dedicada** (Agente 2): quedó sin revisar N+1 en EF Core, `AsNoTracking`, uso de async/await, violaciones de capas, y `any`/`@ts-ignore` en el frontend. Lo que hay en §4 vino de la auditoría de tests.
- **Arquitectura, síntesis** (Agente 4): los datos crudos están (§2 y §5), pero no se revisaron las referencias entre proyectos `.csproj`, el Dockerfile en detalle, ni CI/CD.
- **Mercado y estrategia** (Agente 5): **completamente pendiente** — benchmark competitivo, contexto regulatorio post-DNU 70/2023, gap analysis y estrategia mobile comparada.

**Limitaciones de lo que sí corrió:**
- No se ejecutó `dotnet build`, `dotnet test` ni la app: todas las afirmaciones de cobertura son por inspección estática y grep exhaustivo, no por un informe de coverage. No se verificó si los tests actualmente pasan.
- Los hallazgos de accesibilidad son auditoría de código contra WCAG 2.2 AA, no pruebas con tecnología asistiva real.
- Los problemas de layout móvil están deducidos de valores CSS explícitos, no de capturas en navegador.
- El contraste se calculó sobre colores planos; no se evaluó el peor punto de los gradientes con opacidad.
- La rama `fix/audit-2026-07-18` está en curso y el código referencia auditorías previas (A2, A5-A9, B11-B15, M6-M15). Parte de estos hallazgos puede ya estar en el plan de remediación de `.planning/`, que no se revisó.
