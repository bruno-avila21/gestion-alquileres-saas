# Plan de despliegue — primer entorno visible

**Fecha:** 2026-08-01
**Objetivo:** tener el SaaS corriendo en una URL, para verlo y mostrarlo. No es producción con clientes reales todavía — ver §6.

---

## 1. Qué necesita esta aplicación, según el código

No es un SPA suelto: son cuatro piezas, y una de ellas suele olvidarse.

| Pieza | Requisito | Evidencia |
|---|---|---|
| **API .NET 8** | Contenedor always-on, puerto 8080, no-root | `api/Dockerfile` — multi-stage, `USER app`, `HEALTHCHECK` sobre `/health/live` |
| **PostgreSQL** | Dos connection strings (`DefaultConnection` y `HangfireConnection`; pueden apuntar a la misma base) | `Program.cs:159-160` |
| **Object storage S3** | **Obligatorio.** La app *no arranca* sin él fuera de Development | `SecuritySettingsValidator.cs:62-71` |
| **SPA estática** | `pnpm build` → `dist/`, servida con fallback a `index.html` | `web/package.json` — no hay Dockerfile para el frontend |

Más una dependencia externa: **`indices-api`**, el servicio de índices ICL/IPC (`IndicesApi:BaseUrl`, `appsettings.json:37-40`). Vive en otro repositorio. Sin él, la proyección de ajustes falla — pero el resto de la aplicación funciona.

---

## 2. Las cinco trampas de este despliegue

Ninguna es obvia desde afuera. Las cuatro primeras te van a morder el día uno.

### 2.1 🔴 Hangfire corre dentro del proceso de la API → **no puede haber scale-to-zero**

`Program.cs:166` llama a `AddHangfireServer()` sin condición: el mismo contenedor que atiende HTTP ejecuta los jobs programados (ajustes mensuales, sincronización de índices, avisos de vencimiento, limpieza de refresh tokens).

**Consecuencia:** cualquier plataforma que duerma el contenedor por inactividad — el free tier de Render, `min_machines_running = 0` en Fly — hace que **los jobs no se ejecuten nunca**. La app parece andar y silenciosamente no ajusta alquileres.

**Además:** cada réplica se vuelve un worker que compite por el mismo store de Postgres. **Para este entorno: una sola réplica.**

### 2.2 🔴 `Cors:AllowedOrigins` falla en abierto

`Program.cs:45-53` cae a `http://localhost:5173` si la clave no está configurada, **en cualquier entorno**, y `SecuritySettingsValidator` no la valida. La API arranca sin error y acepta peticiones con credenciales desde localhost.

**Hay que setearla sí o sí**, aunque el frontend esté en el mismo dominio.

### 2.3 🟠 `AllowedHosts: "*"` permite envenenar la URL de descarga

`appsettings.json:23` desactiva el filtrado de host, y `DocumentsController.cs:64-66` arma la URL absoluta de descarga con el header `Host` que mande el cliente. Con un CDN delante, una respuesta envenenada puede servirse a otros usuarios con el token adentro. **Setear `AllowedHosts` al dominio real.**

### 2.4 🟠 `VITE_API_URL` se resuelve en tiempo de BUILD

Vite inlinea las variables al compilar. La URL de la API no se puede cambiar después con una variable de entorno del hosting: **cambiarla exige recompilar el frontend**. Definila antes del primer build.

### 2.5 🟡 Las migraciones NO se aplican solas

`Program.cs` no llama a `Migrate()` — verificado. Es la decisión correcta (evita que N réplicas migren en paralelo), pero significa que **hace falta un paso explícito** en cada deploy que traiga migraciones. Hoy hay 13, incluida `20260731223906_AddSentNotifications`, que todavía no se aplicó a ninguna base.

---

## 2bis. Correrlo gratis (entorno de pruebas)

Para probar antes de vender, **local le gana a la nube gratuita** — y no solo por precio.

### El detalle que lo decide

`Program.cs:233` → `IsReadOnlyFunc = _ => !app.Environment.IsDevelopment()`

**Fuera de Development el panel de Hangfire es de solo lectura: no se puede disparar un job a mano.** En un despliegue gratuito en modo Producción, probar el ajuste mensual de alquileres implicaría esperar al día 1 a las 09:00 o cambiar el cron y volver a desplegar. Corriendo local en Development, el panel es escribible y disparás cualquier job cuando querés.

### Opción gratuita A — todo local con docker compose ✅ implementado

```bash
cp .env.example .env          # completar JWT_SECRET y DOCUMENT_TOKEN_SECRET
docker compose up -d --build
docker compose run --rm migrate
# → http://localhost:8080
```

Levanta Postgres, MinIO (con el bucket `documents` creado), la API y el portal web servido por Caddy. Costo cero, sin límite de horas, sin arranques en frío.

Dos decisiones del armado que vale conocer:

- **Mismo origen.** Caddy sirve el SPA y hace de proxy a `/api`, `/health`, `/hangfire` y `/swagger`. El navegador nunca manda `Origin`, así que **CORS no llega a intervenir** y la cookie HttpOnly de sesión viaja sin configuración extra. `VITE_API_URL` queda en `/api/v1`, relativo.
- **S3 de verdad, no disco local.** El compose apunta `Storage:Provider=S3` a MinIO, así que desarrollo ejercita el mismo camino de código que producción. Un bug de storage aparece acá y no en el despliegue.

Las migraciones van en un contenedor aparte (`profiles: ["tools"]`), porque la API no migra al arrancar. `AppDbContextFactory` trae una cadena de conexión de marcador, así que el stage de migraciones pasa `--connection` explícito.

### Opción gratuita B — local + túnel, para mostrarlo

Cloudflare Tunnel es gratis y expone tu `localhost:8080` en una URL pública temporal. Sirve para una demo puntual sin desplegar nada.

### Opción gratuita C — Oracle Cloud Always Free ✅ implementado

La única nube genuinamente gratuita **sin hibernación**: 4 núcleos ARM y 24 GB de RAM, permanentes. Se corre el mismo compose con el override de producción:

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
docker compose -f docker-compose.yml -f docker-compose.prod.yml run --rm migrate
```

El override pasa a Producción, hace que Caddy escuche en 80/443 y saque el certificado TLS solo contra `SITE_ADDRESS`, cierra el puerto directo de la API y ata Postgres y MinIO a loopback.

El `Dockerfile` de la API usa las imágenes multi-arquitectura de Microsoft, así que corre en ARM Ampere sin cambios. **Contra real:** conseguir capacidad ARM está difícil en varias regiones y el alta de cuenta tiene fricción.

> ⚠️ En modo Producción el panel de Hangfire vuelve a ser de solo lectura. Si el objetivo del VPS es seguir probando y no mostrar, dejalo en Development — pero entonces Swagger queda expuesto y el panel sin autenticación, así que no lo dejes accesible al público.

### La que descartaría

Coser tiers gratuitos administrados (Neon o Supabase + Render + Pages) suena bien y es la peor para probar: la API de Render duerme a los 15 minutos y un .NET tarda cerca de un minuto en despertar, las bases gratuitas se pausan por inactividad, y quedás en modo Producción con el panel bloqueado.

**Salvedad:** las condiciones de los tiers gratuitos cambian seguido y esta información tiene fecha de corte. Verificá los límites vigentes antes de comprometerte.

---

## 3. Opciones pagas, con su costo real

Ordenadas por tiempo hasta la primera URL.

### Opción A — Railway + Cloudflare R2 · **recomendada para arrancar**

| Componente | Servicio | Costo aprox. |
|---|---|---|
| API | Railway, build desde `api/Dockerfile` | ~USD 5-10/mes |
| Postgres | Railway Postgres | incluido en el uso |
| Storage S3 | Cloudflare R2 | **gratis** hasta 10 GB, sin cargo de egreso |
| SPA | Cloudflare Pages | **gratis** |

**A favor:** de cero a URL en una tarde. Railway detecta el Dockerfile, inyecta `DATABASE_URL` y no duerme los servicios pagos. R2 es S3-compatible, así que `Storage:Provider=S3` funciona sin tocar código.
**En contra:** el costo escala con el uso y no es del todo predecible. Región más cercana: `us-east`; no hay São Paulo.

### Opción B — Fly.io + Tigris

| Componente | Costo aprox. |
|---|---|
| Máquina `shared-cpu-1x` 512 MB, siempre encendida | ~USD 5/mes |
| Fly Postgres (1 GB) | ~USD 5/mes |
| Tigris (S3-compatible, integrado) | gratis hasta cierto uso |

**A favor:** región **`gru` (São Paulo)**, la mejor latencia disponible para Argentina. Tigris se provisiona con un comando y ya viene con credenciales S3.
**En contra:** hay que configurar `min_machines_running = 1` explícitamente en `fly.toml`, o §2.1 te muerde.

### Opción C — VPS único con docker compose · **la más barata y la que más se parece a lo que ya tenés**

Un Hetzner CX22 (2 vCPU, 4 GB) sale ~EUR 4/mes. En una sola máquina: Postgres + MinIO + API + Caddy sirviendo el SPA con TLS automático.

**A favor:** el `docker-compose.yml` del repo ya levanta Postgres y MinIO con el bucket `documents` creado (`docker-compose.yml:37-48`). Falta agregarle el servicio `api` y un Caddy. Costo fijo y predecible, control total.
**En contra:** el mantenimiento del servidor es tuyo (backups, actualizaciones, TLS si no usás Caddy). Hetzner no tiene región en Sudamérica.

> **Mi recomendación:** empezá por **A**. El objetivo ahora es *ver la aplicación funcionando*, no optimizar infraestructura. Cuando haya clientes reales, **C** en un VPS con backups, o **B** si la latencia empieza a importar.

---

## 4. Variables de entorno — la lista completa

En .NET, el separador de sección en variables de entorno es **doble guion bajo**. Faltando cualquiera de las marcadas 🔴, la aplicación **no arranca** (y eso es deliberado).

```bash
ASPNETCORE_ENVIRONMENT=Production

# 🔴 Base de datos — no puede contener "devpassword"
ConnectionStrings__DefaultConnection=Host=...;Port=5432;Database=gestion_alquileres;Username=...;Password=...
ConnectionStrings__HangfireConnection=Host=...;Port=5432;Database=gestion_alquileres;Username=...;Password=...

# 🔴 Secretos — mínimo 32 caracteres, sin la cadena "REPLACE_WITH"
#    Generar con: openssl rand -base64 48
JwtSettings__SecretKey=<48+ chars aleatorios>
DocumentToken__Secret=<48+ chars aleatorios>

# 🔴 Storage — Provider DEBE ser S3 fuera de Development
Storage__Provider=S3
Storage__Bucket=documents
Storage__ServiceUrl=https://<account>.r2.cloudflarestorage.com   # vacío si es AWS S3 real
Storage__AccessKey=<key>
Storage__SecretKey=<secret>
Storage__ForcePathStyle=true
Storage__Region=us-east-1

# 🔴 CORS — sin esto cae a localhost y acepta credenciales (§2.2)
Cors__AllowedOrigins__0=https://app.tudominio.com

# 🟠 Host filtering (§2.3)
AllowedHosts=api.tudominio.com

# Índices ICL/IPC — servicio externo
IndicesApi__BaseUrl=https://indices.tudominio.com
IndicesApi__ApiKey=<key>

# Email — "Null" NO envía nada (§6). Para que salgan mails:
Email__Provider=Smtp
Email__Host=smtp.resend.com
Email__Port=587
Email__UseSsl=true
Email__Username=resend
Email__Password=<api key>
Email__FromEmail=no-reply@tudominio.com
```

**Frontend** (en tiempo de build, no de runtime):

```bash
VITE_API_URL=https://api.tudominio.com/api/v1
```

---

## 5. Pasos concretos

### Fase 1 — Infraestructura (~1 hora)
1. Crear el proyecto en Railway y agregarle **PostgreSQL**.
2. Crear un bucket **R2** llamado `documents` y generar un token de API con permiso de lectura y escritura. Anotar `ServiceUrl`, `AccessKey` y `SecretKey`.
3. Generar los dos secretos: `openssl rand -base64 48`, uno para JWT y otro distinto para el token de documentos.

### Fase 2 — API (~1 hora)
4. Nuevo servicio en Railway apuntando al repo, **root directory `api/`**, que detecta el `Dockerfile`.
5. Cargar todas las variables de §4.
6. **Una sola réplica** (§2.1). Verificar que el servicio no tenga sleep-on-idle.
7. Aplicar las migraciones. Desde tu máquina, con la connection string de producción:
   ```bash
   cd api
   dotnet ef database update \
     --project src/GestionAlquileres.Infrastructure \
     --startup-project src/GestionAlquileres.API \
     --connection "<connection string de producción>"
   ```
   Hangfire crea sus propias tablas solo al arrancar.
8. Comprobar `GET /health/ready` → debe responder `{"status":"ok","db":"up"}`. Si devuelve 503, la base no está accesible.

### Fase 3 — Frontend (~30 min)
9. Proyecto en Cloudflare Pages sobre el mismo repo:
   - Root: `web`
   - Build: `pnpm install --frozen-lockfile && pnpm build`
   - Output: `dist`
   - Variable: `VITE_API_URL=https://<url-de-la-api>/api/v1`
10. Configurar el **fallback SPA a `index.html`** — sin esto, recargar en `/admin/contratos` da 404. En Pages: un archivo `_redirects` con `/* /index.html 200`.
11. Volver a la API y setear `Cors__AllowedOrigins__0` con la URL final de Pages. Redeploy.

### Fase 4 — Verificación (~30 min)
12. Registrar una organización desde la UI y entrar.
13. Crear propiedad → inquilino → contrato.
14. **Subir un documento y descargarlo.** Es la prueba de fuego del storage: si R2 está mal configurado, falla acá.
15. Registrar un pago y ver que aparezca en el estado de cuenta.
16. Abrir `/hangfire` (pide rol Admin fuera de Development) y confirmar que los cuatro jobs recurrentes están registrados.
17. Abrir el portal de inquilinos en un teléfono real.

### Fase 5 — Automatizar (opcional)
18. Extender `.github/workflows/ci.yml`. Hoy corre **solo en `master`/`main` y en pull requests**, así que en esta rama no se está ejecutando, y no despliega nada. Agregar un job de deploy sobre `master`, más un paso de migraciones.

---

## 6. Antes de mostrárselo a alguien de afuera

Este entorno sirve para **verlo y demostrarlo**. Todavía no para clientes reales con datos reales, por hallazgos abiertos de la auditoría:

| Hallazgo | Qué implica acá |
|---|---|
| **No existe cambio ni recuperación de contraseña** | Si alguien pierde la clave, no hay forma de recuperarla salvo tocar la base |
| **El logout no invalida el token (8 h de vida)** | Cerrar sesión no corta el acceso real |
| **Dar de baja a un inquilino no le quita el acceso** | Sigue entrando al portal indefinidamente |
| **Alta de organizaciones abierta y sin verificar email** | Cualquiera con la URL crea organizaciones y ocupa slugs de forma irrecuperable |
| **`Email:Provider` en `Null` por defecto** | No se envía ningún mail. Si querés probar los avisos, configurá SMTP (§4) |
| **Rate limiting por IP detrás de proxy** | Sin `UseForwardedHeaders`, los 20 req/min son un cupo global compartido |

**Mitigación mientras tanto:** mantené la URL sin difundir, o poné el frontend detrás de Cloudflare Access. Y si va a ser públicamente accesible, deshabilitá o protegé `POST /api/v1/auth/register-org`.

---

## 7. Costo estimado del entorno de demostración

| Concepto | Mensual |
|---|---|
| Railway (API + Postgres, uso bajo) | USD 5-10 |
| Cloudflare R2 (< 10 GB) | USD 0 |
| Cloudflare Pages | USD 0 |
| Dominio `.com.ar` | ~USD 2 (anual prorrateado) |
| **Total** | **~USD 5-12/mes** |

La alternativa del VPS (opción C) queda en ~EUR 4/mes fijos, a cambio de un rato de configuración inicial y el mantenimiento a cargo tuyo.
