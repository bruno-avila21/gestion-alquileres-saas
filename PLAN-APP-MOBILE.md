# Plan — App mobile (APK) para inquilinos

**Fecha:** 2026-07-31
**Decisiones tomadas:** una sola APK para todas las inmobiliarias (branding en runtime) · v1 solo para el perfil inquilino
**Estado:** plan diferido. Ver la nota de secuencia abajo.

> ### ⚠️ Secuencia revisada (2026-07-31)
>
> Tras la auditoría de mercado se decidió **resecuenciar: la APK se pospone al tramo de 12 meses.** La arquitectura de este documento no cambia — quedó validada por el benchmark (Buildium y AppFolio hacen exactamente una app compartida con branding en runtime, y es la única lectura de la guideline 4.2.6 de Apple que no exige cuenta de desarrollador por inmobiliaria). Lo que cambia es **cuándo** y **con qué adentro**.
>
> **Motivo:** sin pago dentro de la app, una app que se abre una vez al mes para fotografiar un comprobante compite en desventaja con WhatsApp — que el inquilino ya tiene instalado y no exige recordar una contraseña. El estándar de la categoría es *pagar* y *abrir un ticket*.
>
> ### ✅ Fase 0 ejecutada (2026-07-31)
>
> | Bloqueante | Estado |
> |---|---|
> | #1 Storage | ✅ Diagnóstico corregido — producción ya no podía arrancar con storage local. Se arregló el `BasePath: ""` que rompía el upload en desarrollo, se documentó la config contra MinIO y se agregó canonicalización de path |
> | #2 Fechas | ✅ `parseApiDate` distingue `DateOnly` de `DateTimeOffset`; 3 vistas que formateaban un período con `formatDate` pasaron a `formatPeriod` |
> | #3 Job de vencimientos | ✅ Tabla `sent_notifications` con índice único + `[DisableConcurrentExecution]` + `ArgentinaTime` en la ventana |
> | #6 Base PWA | ✅ `lang="es-AR"`, título real, manifest, theme-color. **Falta**: íconos PNG 192/512 maskable para que Chrome ofrezca instalar |
> | #5 Nav del inquilino | ✅ Componente único montado en el layout, `NavLink` con `aria-current`, indicador no cromático, targets de 48px y cierre de sesión con confirmación |
>
> Bloqueantes #7 a #12 (ambigüedad de `/me`, refresh muerto, cambio de contraseña, revocación de sesión, baja de inquilino, `AllowedHosts`) siguen abiertos: pertenecen al tramo previo a la APK, no a los 0-3 meses.
>
> **Orden acordado:**
> 1. **0-3 meses:** críticos de auditoría · motor de ajustes completo (% fijo escalonado, Casa Propia/ICP, frecuencia cuatrimestral y semestral) · WhatsApp · punitorios automáticos · PDF de recibo y liquidación
> 2. **6 meses:** MercadoPago + conciliación · PWA instalable del portal de inquilino · login por link firmado
> 3. **12 meses:** esta APK, con pago y mantenimiento adentro
>
> **Criterio de decisión:** si el portal PWA de inquilino no supera 35-40% de MAU sobre inquilinos activos, la APK no va a mover esa aguja — el problema sería de valor, no de empaquetado.
>
> Los bloqueantes de §1 siguen vigentes: varios (storage persistente, fechas, revocación de sesión) son prerequisito también del tramo de 0-3 meses.

---

## 0. Arquitectura elegida

**Capacitor sobre el build de React que ya existe.**

Razón: el portal del inquilino ya está diseñado mobile-first — contenedor de 420 px, tabs inferiores fijos y bottom sheet ya implementados (`web/src/portal-inquilino/pages/HomePage.tsx:60`, `DocumentosPage.tsx:172-181`). Capacitor envuelve ese build sin reescribir nada, y aporta lo que la web no da: push por FCM, cámara nativa para el comprobante, y almacenamiento seguro del token en el Keystore de Android.

Descartados:
- **React Native / Expo** — obligaría a reescribir toda la UI (React DOM ≠ React Native). Meses de trabajo para llegar al mismo lugar.
- **TWA puro** — no da acceso a Keystore ni a plugins nativos, y Google puede objetar una APK que sea solo un webview de un sitio.

```
┌─────────────────────────────────────────┐
│  APK "Alquilar.io"  (1 sola en Play)    │
│  ┌───────────────────────────────────┐  │
│  │  WebView  ← bundle React local    │  │
│  │  tema aplicado en runtime         │  │
│  └───────────────────────────────────┘  │
│  Plugins: Push (FCM) · Camera ·         │
│  SecureStorage · AppLinks · Updater     │
└──────────────┬──────────────────────────┘
               │ HTTPS
        ┌──────▼──────┐
        │  API .NET 8 │  branding por slug
        │             │  push tokens
        │             │  reportes de pago
        └─────────────┘
```

---

## 1. Bloqueantes — hay que resolverlos ANTES de empezar la app

Salen de la auditoría del 2026-07-31. No son "nice to have": cada uno rompe la app mobile de forma directa.

| # | Bloqueante | Ubicación | Por qué bloquea |
|---|---|---|---|
| 1 | **Storage efímero** — provider `Local` por defecto sin volumen | `DependencyInjection.cs:89` · `appsettings.json:28` · `Dockerfile:17` | La app sube fotos de comprobantes y sirve logos de inmobiliarias. Con storage que se borra en cada deploy, ambas features nacen rotas |
| 2 | **Fechas corridas un día** | `web/src/shared/lib/formatters.ts:6-7` | Verificado: `formatDate('2026-01-01')` → "31 de dic de 2025". En un push que dice "tu alquiler vence mañana" el error es mucho más grave que en una tabla |
| 3 | **Job de vencimientos no idempotente** | `ContractExpiryNotificationJob.cs:9,29-30` | Hoy manda el mismo mail 30 días seguidos. Si le conectamos push sin arreglarlo, son **30 notificaciones push** al mismo inquilino. Desinstalación garantizada |
| 4 | **Sin presigned URLs** | `grep PreSigned` sobre `api/src` → cero · `FilesController.cs:37-38` | Todo byte de todo documento pasa por el proceso de la API. En red móvil y con fotos, no escala |
| 5 | **Nav del inquilino duplicada 4 veces, sin logout** | `HomePage.tsx:14-48` · `ContratoPage.tsx:7-25` · `PagosPage.tsx:18-36` · `DocumentosPage.tsx:13-31` · `InquilinoLayout.tsx:9-13` | La app necesita una nav única con "Cuenta". Hoy son cuatro copias divergentes y no existe forma de cerrar sesión |
| 6 | **Sin base PWA** | `web/index.html` sin manifest ni service worker · `lang="en"` · `<title>web-scratch</title>` | Trabajo que hay que hacer igual para cualquier camino |
| 7 | **Los 4 handlers de `/me` resuelven el contrato activo de forma ambigua, y `/me/transactions` no pagina** | `GetMyContractQueryHandler.cs:25-26` · `GetMyTransactionsQueryHandler.cs:30-31,34` · `GetMyRentHistoryQueryHandler.cs:30-31` · `GetMyDocumentsQueryHandler.cs:29-30` | **`/me/*` ES la superficie de la v1.** Los cuatro repiten `ListAsync(...).FirstOrDefault()` con orden `CreatedAt DESC`: un inquilino con dos contratos activos ve datos del más nuevo sin indicio del otro, y cada handler puede desincronizarse de los demás |
| 8 | **El cliente nunca llama a `/auth/refresh`** | `web/src/shared/lib/api.ts:17-24` — grep de "refresh" sobre `web/src` da cero | El refresh es el mecanismo central de sesión de la APK. Hoy es código muerto del lado cliente: hay que construirlo y probarlo desde cero |
| 9 | **No existe cambio ni reseteo de contraseña** | `AuthController.cs:25-105` — no hay `change-password` ni `forgot-password` · `User.cs:10` sin `MustChangePassword` | La contraseña temporal del inquilino es su credencial **permanente**. En una app pública de Play Store esto es insostenible: no hay recuperación posible y nadie puede rotarla |
| 10 | **Token de acceso de 8 h sin revocación; el logout es cosmético** | `JwtService.cs:35-40` · `appsettings.json:10` · `AuthController.cs:90-96` | En la web el token muere al cerrar el navegador. **En un teléfono vive en disco**: un dispositivo robado da 8 h de acceso a los datos financieros de la inmobiliaria, y ningún logout lo corta |
| 11 | **Dar de baja a un inquilino no revoca su acceso** | `DeleteAppTenantCommandHandler.cs:18` · `UpdateAppTenantCommandHandler.cs:27` | No tocan el `User` vinculado ni revocan refresh tokens. Un ex-inquilino sigue entrando a la app indefinidamente |
| 12 | **`AllowedHosts: "*"` + URL de descarga armada con el header `Host`** | `appsettings.json:23` · `DocumentsController.cs:64-66` | Con branding cacheado por `slug` y un CDN delante, es el vector para que el branding (o un token de descarga) de una inmobiliaria se sirva a los usuarios de otra |

---

## 2. Branding por inmobiliaria

### 2.1 Esquema — no existe nada hoy

`organizations` tiene exactamente seis columnas: `id`, `name`, `slug`, `plan`, `is_active`, `created_at` (`Migrations/20260413022934_InitialCreate.cs:14-28`). Cero campos de branding.

Campos a agregar (migración nueva sobre `organizations`, o tabla `organization_branding` 1:1 si se prefiere aislar):

| Campo | Tipo | Notas |
|---|---|---|
| `display_name` | `varchar(80)` | Nombre comercial visible en la app (distinto de `name`, que es el legal) |
| `logo_storage_key` | `varchar(512)` NULL | Apunta al storage; el logo es público, no lleva token |
| `primary_color` | `char(7)` NULL | Hex `#RRGGBB` |
| `accent_color` | `char(7)` NULL | Hex `#RRGGBB` |
| `support_email` | `varchar(320)` NULL | Para "Contactar a mi inmobiliaria" |
| `support_phone` | `varchar(30)` NULL | Idem |
| `support_whatsapp` | `varchar(30)` NULL | Canal principal en Argentina |
| `updated_at` | `timestamptz` | **Ver §6** — el esquema no tiene `updated_at` en ninguna tabla |

### 2.2 Endpoint público de branding

```
GET /api/v1/orgs/{slug}/branding   →   [AllowAnonymous]
```

Tiene que ser anónimo: la app necesita pintarse **antes** de que el usuario se autentique.

Consideraciones de seguridad, no opcionales:

- **Validar los colores con regex estricto** `^#[0-9a-fA-F]{6}$` en el validator, no solo en el frontend. Un color que se inyecta en un `style` sin validar es un vector de inyección de CSS. Este es el riesgo real de "branding servido por API".
- **Cero HTML, CSS o JS arbitrario.** Nada de mensajes de bienvenida con texto enriquecido ni CSS custom. Como la APK es **una sola binaria compartida por todas las inmobiliarias**, contenido inyectable por la Inmobiliaria A correría con los permisos de la app de los usuarios de todas las demás. Tipografías: enum cerrado, no string libre.
- **Logos: validar magic bytes y dimensiones del lado servidor, y rechazar SVG** (es XML ejecutable). Solo PNG/WebP. Servirlos desde un dominio o bucket separado, sin cookies.
- **Rate limiting** sobre este endpoint: sin él es un enumerador de slugs de clientes. Ya existe la infraestructura (`Program.cs:143-156`).
- **Devolver solo datos públicos**: nombre comercial, logo, colores, canales de contacto. Nada de `plan`, `is_active`, conteos ni ids internos. Cualquier campo de más es un canal de fuga entre tenants.
- **Logo servido como archivo público** — es un logo, no un documento privado. No debe pasar por el flujo de tokens HMAC de `DocumentTokenService`.
- **Resolver `AllowedHosts: "*"` antes de montar el pipeline** (bloqueante #12). Con branding cacheado por `slug` y un CDN delante, el host reflejado permite servir el branding de una inmobiliaria a los usuarios de otra.
- Después del login, el branding se deriva del `org_id` del JWT, **nunca** de un parámetro. Y ningún dato que llegue por deep link puede cambiar el contexto de tenant.

### 2.3 Aplicación del tema en el cliente

El design system ya está montado sobre variables CSS (`web/src/index.css`), y `--brand` ya se sobrescribe por portal con `.t-portal` (`index.css:333`). Aplicar el tema de una inmobiliaria es sobrescribir esas variables en `:root` al resolver la organización — no hace falta tocar ni un componente.

⚠️ Con la salvedad del hallazgo de la auditoría de UX: hay estilos inline con colores literales en las pantallas clave que **no** responden a las variables. Habría que migrarlos a tokens para que el branding se aplique de verdad en toda la app.

---

## 3. Onboarding sin pedir el slug

**Problema:** `TenantLoginCommand` exige `OrganizationSlug`. En web ya es fricción (la auditoría de UX lo marcó como jerga incomprensible en `LoginForm.tsx:7,37`). En una APK bajada de Play Store, pedirle a un inquilino que escriba el slug de su inmobiliaria es una barrera de abandono en la primera pantalla.

**Solución: deep link de invitación.** Resuelve el slug y el branding de una sola vez.

```
Admin invita al inquilino
  └─> API genera token de invitación de un solo uso, con vencimiento
       └─> se envía por WhatsApp / email:  https://app.alquilar.io/i/{token}
            ├─ APK instalada  → Android App Links abre la app directamente
            └─ sin instalar   → landing web con botón a Play Store,
                                y el token sobrevive a la instalación
                 └─> la app resuelve: organización + branding + email
                      └─> el inquilino solo define su contraseña
```

Esto reemplaza el flujo actual, donde `POST /api/v1/tenants/{id}/invite` devuelve una contraseña temporal en texto plano que el admin tiene que copiar y pasar a mano (`InviteTenantResult`, mostrado en `InquilinosPage.tsx:157`). Mejora también la seguridad: hoy esa contraseña temporal no tiene vencimiento ni forzado de cambio.

Requiere `assetlinks.json` publicado en el dominio para que Android App Links funcione sin el diálogo de desambiguación.

---

## 4. Push notifications (FCM)

### Backend
- Tabla `device_tokens`: `user_id`, `organization_id`, `token`, `platform`, `created_at`, `last_seen_at`. Con índice único sobre `token` y borrado de tokens muertos cuando FCM los reporta inválidos.
- `POST /api/v1/me/devices` y `DELETE /api/v1/me/devices/{token}`.
- Servicio de envío, con la misma disciplina que falta hoy en el email: timeout, reintento y registro de fallos.

### Disparadores de la v1
| Evento | Origen | Prerequisito |
|---|---|---|
| Ajuste aplicado | Ya hay hook de notificación en `ApplyRentAdjustmentCommandHandler.cs:166-186` | — |
| Vencimiento próximo | `ContractExpiryNotificationJob` | **Bloqueante #3** — arreglar idempotencia primero |
| Pago registrado / confirmado | `RegisterPaymentCommandHandler` | — |
| Índice publicado | `SyncIndexesJob` | Solo si se decide notificar al inquilino |

Los envíos deben salir por un job de Hangfire encolado, nunca inline en el request — el email hoy se manda de forma síncrona sobre el hilo HTTP (`ApplyRentAdjustmentCommandHandler.cs:166`) y suma la latencia del relay a la respuesta del endpoint. No repetir ese patrón con push.

---

## 5. "Informar pago" con foto — la feature que justifica la app

Hoy el portal del inquilino es **100% consulta**: el único botón de acción de toda la app dice "próximamente" (`ContratoPage.tsx:156-163`). Esta es la feature que convierte la app en algo que se abre en vez de consultarse.

**Decisión de dominio importante:** un reporte de pago **no** debe crear una `Transaction` directamente. Si lo hiciera, el inquilino estaría escribiendo en la contabilidad de la inmobiliaria. El flujo correcto:

```
Inquilino: foto + monto + período
   └─> POST /api/v1/me/payment-reports   (multipart)
        └─> entidad PaymentReport, estado = Pendiente
             └─> push al admin
                  └─> Admin confirma o rechaza
                       └─> al confirmar: se crea la Transaction de tipo Payment
                            └─> push de confirmación al inquilino
```

Necesita pantalla nueva en el portal admin para la bandeja de reportes pendientes. Y depende del **bloqueante #1** (storage persistente).

---

## 6. Sincronización — el prerequisito silencioso

**Ninguna tabla del esquema tiene `updated_at`.** Verificado: grep de `updated_at`/`UpdatedAt` sobre `api/src/**/*.cs` devuelve cero resultados; `created_at` sí está en las 11 tablas.

Sin marca de última modificación no hay forma de pedir "dame lo que cambió desde X", así que la app tendría que redescargar todo en cada apertura. Para la v1 con un solo contrato por inquilino es tolerable; en cuanto se agregue el perfil admin (que lista cientos de contratos) deja de serlo.

Recomendación: agregar `updated_at` con trigger o interceptor de `SaveChanges` en la misma tanda de migraciones del branding. Es barato ahora y caro después.

Además hay que unificar la paginación: hoy conviven **tres formas distintas** — `TransactionsPageDto` (con `netBalance`), `PagedResult<T>`, y arrays planos sin paginar en `/contracts`, `/properties`, `/tenants`, `/owners` y todos los `/me/*`. Y `PagedResult` no expone `totalPages` ni `hasNext` (`Common/DTOs/PagedResult.cs:8`), así que el scroll infinito hay que derivarlo a mano.

---

## 7. Autenticación en el WebView

**Punto delicado.** El refresh token hoy vive en una cookie HttpOnly (`AuthController.cs:77`). En un WebView de Capacitor el origen es un esquema local, no el dominio de la API, así que esa cookie no viaja de forma confiable.

La buena noticia: el endpoint **ya está preparado** — `RefreshRequest(string? RefreshToken)` acepta el token por body de forma opcional (`AuthController.cs:23`), priorizando la cookie cuando está. Solo hay que usar esa vía en el cliente nativo.

Reglas para el cliente nativo:
- El refresh token va a **`EncryptedSharedPreferences` respaldado por el Android Keystore**, nunca a `localStorage` ni a `Preferences` en texto plano.
- `android:allowBackup="false"` y exclusión explícita del token de las reglas de backup — si no, el secreto se sincroniza a la nube del usuario.
- `android:usesCleartextTraffic="false"` explícito, y `android:exported="false"` en toda actividad que no necesite ser pública.
- El access token vive solo en memoria.
- **Bajar `ExpiryHours` de 8 a 15-30 minutos.** Hoy está en 8 justamente porque ningún cliente usa el refresh (bloqueante #8): resolver eso habilita acortar la ventana, que es la causa raíz del bloqueante #10.
- **Vincular el refresh token al dispositivo**: persistir un `DeviceId` junto al hash en `refresh_tokens` y rechazar el canje desde otro dispositivo. La detección de reuso con revocación de familia ya existe y está bien implementada (`RefreshAccessTokenCommandHandler.cs:48-57`) — con device binding pasa a cubrir el clonado de la app.
- **Claim de versión de sesión (`sec_stamp`)** en el JWT, comparado contra el `User` en `OnTokenValidated`, y bumpeado en logout, cambio de contraseña y desactivación. Sin esto no hay logout remoto ni forma de expulsar un teléfono robado.
- **Tabla de dispositivos/sesiones activas** por usuario, con pantalla de gestión y revocación selectiva. Se solapa con la tabla `device_tokens` de push (§4): conviene diseñarlas juntas.
- **Certificate pinning** con Network Security Config (`<pin-set>` con pin de backup y fecha de expiración). Requiere resolver antes el flag `Secure` de las cookies: hoy se deriva de `Request.IsHttps` (`AuthController.cs:123`), que detrás de un proxy que termina TLS es `false`.
- La verificación de rol del lado cliente es cosmética. La APK es código público: cualquier secreto embebido se recupera con `apktool`.

---

## 8. Actualización sin pasar por Play Store

El requisito de "actualizable" se cumple con **bundle local + updater OTA**: el APK trae el bundle web embebido, chequea versión contra el servidor al arrancar y descarga el nuevo si corresponde.

Descartado apuntar el WebView directo a la URL remota (`server.url` en la config): es lo más simple, pero pierde el arranque offline y expone a que Google rechace la app por ser un contenedor de un sitio web.

Lo que **sí** requiere pasar por Play Store: cambios de plugins nativos, permisos nuevos, o subir la versión del runtime. La UI y la lógica de negocio se actualizan solas.

---

## 9. Fases

| Fase | Contenido | Depende de |
|---|---|---|
| **0 — Destrabar** | Storage persistente · fechas de `formatters.ts` · idempotencia del job de vencimientos · manifest + `lang="es-AR"` · nav única del inquilino con logout | — |
| **1 — Branding** | Migración de campos · `GET /orgs/{slug}/branding` con validación de hex y rate limit · carga de logo desde el admin · aplicación del tema en runtime · migrar estilos inline a tokens | Fase 0 (storage) |
| **2 — Shell Android** | Proyecto Capacitor · secure storage · refresh por body · App Links + `assetlinks.json` · splash y ícono · pipeline de firma | Fase 1 |
| **3 — Push** | Tabla `device_tokens` · endpoints de registro · servicio FCM encolado en Hangfire · disparadores | Fase 0 (job idempotente) |
| **4 — Informar pago** | Entidad `PaymentReport` · endpoint multipart · cámara nativa · bandeja de confirmación en el admin | Fase 0 (storage) |
| **5 — OTA + release** | Updater de bundle · versionado · listing de Play Store · política de privacidad (obligatoria) | Fase 2 |

**Fase 0 no es negociable.** Construir sobre storage efímero, fechas corridas y un job que manda 30 notificaciones significa lanzar una app rota el primer día.

---

## 10. Pendiente de decidir

- **Deep link:** dominio y esquema definitivos (`app.alquilar.io/i/{token}`), y quién hostea el `assetlinks.json`.
- **Cuenta de Play Store:** a nombre de quién se publica.
- **Updater OTA:** solución concreta (hay opciones open source y comerciales). Conviene verificar el estado actual de cada una contra su documentación antes de comprometerse — este plan no fija versiones ni paquetes específicos a propósito.
- **iOS:** el plan es solo Android (APK). Capacitor deja la puerta abierta con el mismo código, pero implica cuenta de Apple Developer y otro ciclo de review.
- **Política de privacidad y Data Safety de Play Store:** obligatorias para publicar, y la app maneja datos financieros y personales. Hay que redactarlas.
