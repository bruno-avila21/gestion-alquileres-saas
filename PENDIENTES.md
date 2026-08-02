# Pendientes

Lista única y priorizada de lo que queda. El detalle de cada punto, con evidencia y escenario de
falla, está en `AUDITORIA-2026-07-31.md`.

**Última actualización:** 2026-08-01

---

## Ya cerrado

| | Qué | Dónde |
|---|---|---|
| ✅ | Fuga de documentos entre contratos (autorización después de escribir) | `DocumentsController.cs:52` |
| ✅ | Todas las fechas se mostraban un día antes | `formatters.ts` |
| ✅ | El job de vencimientos mandaba el mismo mail 30 días seguidos | `ContractExpiryNotificationJob.cs` |
| ✅ | `BasePath: ""` rompía el primer upload · falta de canonicalización de path | `LocalFileStorageService.cs` |
| ✅ | El código de `Storage/` estaba fuera del repositorio | `.gitignore` |
| ✅ | Idioma del documento en inglés · sin manifest | `web/index.html` |
| ✅ | Nav del inquilino duplicada 4× · sin cerrar sesión | `TenantBottomNav.tsx` |
| ✅ | Cookies sin `Secure` y rate limit por IP de proxy detrás de TLS | `Program.cs` — `UseForwardedHeaders` |
| ✅ | Stack completo con docker compose, local y para VPS | `docker-compose*.yml` |
| ✅ | Ajuste por % fijo · frecuencias cuatrimestral y semestral · `IsInEnum` | `AdjustmentType.cs`, `ContractRules.cs` |
| ✅ | Mapeo frecuencia→meses duplicado en 3 lugares y ya divergente | `AdjustmentFrequencyExtensions.cs` |
| ✅ | Cadena de revocación: refresh en el cliente · token de 8 h → 15 min · baja efectiva del inquilino | `api.ts`, `JwtSettings.cs`, `TenantAccessRevoker.cs` |
| ✅ | Cambio de contraseña, forzado en el primer ingreso, y re-invitación que rota la credencial | `ChangePasswordCommandHandler.cs`, `InviteTenantCommandHandler.cs` |

---

## Bloque 1 — Que se pueda vender (0-3 meses)

Sale del análisis de mercado. Es lo que hoy hace perder un cliente en la demo.

- [x] ~~**Motor de ajustes: % fijo y frecuencias nuevas.**~~ ✅ Se agregaron `FixedPercent`,
      `FourMonthly` y `SemiAnnual`, con el porcentaje pactado en el contrato. Ya se puede cargar
      "8% trimestral" y "IPC cuatrimestral", que son los contratos mayoritarios post-DNU.
      El escalonado además se proyecta localmente, sin depender de indices-api.
- [ ] **Motor de ajustes: lo que falta.**
      - **Casa Propia / ICP** como índice — depende de que indices-api lo sirva.
      - **Escalonado con porcentajes distintos por tramo** (10%, luego 8%, luego 7%): hoy el
        porcentaje es único para todo el contrato.
      - **Tope y piso de ajuste**, y **contratos en USD con cotización**.
      - Mover los índices de enum compilado a **configuración de datos**, para que sumar uno nuevo
        sea una fila y no un deploy (riesgo de reversión regulatoria).
- [ ] **Punitorios e intereses automáticos.** 73% de los hogares inquilinos tiene deudas; es el
      dolor número uno del administrador y la competencia ya lo vende.
      → falta tipo en `TransactionType.cs:3`, tasa en `Contract`, job de devengamiento
- [ ] **WhatsApp como canal.** Recordatorio de vencimiento, aviso de ajuste, envío de recibo, aviso
      de mora. Hoy el proveedor de email por defecto es un no-op: **no sale ningún mensaje**.
      → `appsettings.json:42`
- [ ] **Recibos y liquidaciones en PDF** con la marca de la inmobiliaria. Es el entregable físico
      del negocio; sin esto siguen armándolo en Word. → no hay librería de PDF en el proyecto
- [ ] **Planes con límites reales.** `Organization.Plan` existe con valor `"free"` y no se usa en
      ningún lado: hoy el producto no le puede cobrar a nadie. → `Organization.cs:8`

---

## Bloque 2 — Seguridad abierta

Los tres primeros son el mismo problema visto de tres formas: **no hay manera de revocarle el
acceso a nadie.** Se resuelven en cadena.

- [x] ~~**No existe cambio de contraseña.**~~ ✅ `POST /auth/change-password`, cambio forzado en el
      primer ingreso cuando la credencial la generó el sistema, y re-invitación que regenera la
      contraseña temporal. Cambiarla cierra las sesiones abiertas en otros dispositivos.
- [ ] **Falta recuperación de contraseña ("olvidé mi clave").** Depende de tener email funcionando,
      que está en el bloque 1. Mientras tanto la vía es re-invitar desde el panel, que regenera la
      contraseña temporal.
- [ ] **El portal del inquilino no tiene punto de entrada al cambio voluntario.** La ruta
      `/inquilino/cambiar-clave` existe y el cambio forzado funciona, pero no hay enlace: la nav
      inferior ya tiene cinco ítems y un sexto no entra. Necesita una pantalla de "Cuenta".
- [x] ~~**El logout no invalida el token** (8 h de vida)~~ ✅ El access token pasó de 8 horas a
      **15 minutos**, que es la ventana de revocación real del sistema. Ver la nota abajo.
- [x] ~~**Dar de baja a un inquilino no le quita el acceso**~~ ✅ La baja y la desactivación ahora
      desactivan el `User` vinculado y revocan sus refresh tokens.
- [x] ~~**El cliente nunca llama a `/auth/refresh`**~~ ✅ Interceptor con renovación y reintento,
      deduplicando las llamadas concurrentes.
- [x] ~~**Reactivar un inquilino no le devuelve el acceso.**~~ ✅ Resuelto por la re-invitación, que
      reactiva el usuario y le entrega una contraseña nueva. Reactivar desde la edición sigue sin
      restaurar el acceso — es deliberado: devolverlo exige emitir una credencial, no sólo tildar
      una casilla.

> **Nota sobre el modelo de sesión.** El access token es autocontenido: mientras no expire vale
> aunque el usuario cierre sesión o lo den de baja. Por eso la ventana de revocación del sistema
> **es** su duración, ahora 15 minutos. La continuidad de la sesión la da el refresh token, que sí
> se verifica contra la base en cada canje. Un corte instantáneo requeriría validar un sello de
> sesión contra la base **en cada request**, lo que hoy no tiene caché que lo sostenga.
- [ ] **`Organization.IsActive` no se lee nunca**: no se puede suspender una inmobiliaria morosa.
- [ ] **Alta de organizaciones abierta y sin verificar email**: se pueden ocupar slugs de marcas
      reales de forma irrecuperable. → `AuthController.cs:25`
- [ ] **`AllowedHosts: "*"`** permite envenenar la URL de descarga con el header `Host`.
- [ ] **`Cors:AllowedOrigins` falla en abierto** si no está configurada. Mitigado en el compose
      (mismo origen), pero el agujero sigue en el código. → `Program.cs:45-53`
- [ ] Política de contraseñas sólo por longitud · sin defensa anti-CSRF · token de descarga sin
      vínculo al usuario · `ExistsForPeriodAsync` sin filtro de organización.

---

## Bloque 3 — Correctitud

- [ ] **División por cero** si el índice base vale 0: 500 en vez de error de negocio.
      → `ApplyRentAdjustmentCommandHandler.cs:125-126`
- [ ] **La fecha efectiva por defecto usa UTC** en vez de hora argentina, y en el borde de fin de mes
      eso **selecciona el índice equivocado**. → `ApplyRentAdjustmentCommandHandler.cs:64`
- [x] ~~**Enums de contrato sin `IsInEnum()`**~~ ✅ Validado en alta y edición, junto con el tope de
      `Notes` que devolvía 500 en vez de 400. Las reglas quedaron compartidas en `ContractRules`.
- [ ] **`UpdateContract` no revalida que la propiedad sea de la organización.** Los listados usan
      *inner join*, así que las transacciones de ese contrato **desaparecen de las pantallas sin
      error**. → `UpdateContractCommandHandler.cs:22-23`
- [ ] **`MonthlyRentAdjustmentJob` no tiene un solo test**, y es el disparador de todo el negocio.
      Tampoco hay ningún test que aplique un ajuste **IPC**.
- [ ] **Exports CSV truncados a 500 filas sin aviso**: la conciliación contable sale mal y el error
      no se ve en el archivo. → `TransactionRepository.cs:38-43`
- [ ] **Los cuatro handlers de `/me` resuelven el contrato activo de forma ambigua** y
      `/me/transactions` no pagina. Es la superficie del portal del inquilino.
- [ ] **Deriva de cadencia** en contratos que arrancan el 29, 30 o 31 · `newRent` puede quedar en 0 ·
      redondeo bancario sin decidir.
- [ ] Rendimiento: cero `AsNoTracking()` en todo el backend · el dashboard trae la cartera completa
      para calcular tres números · N+1 en la liquidación al propietario · búsqueda no sargable con
      triple escaneo por request.
- [ ] `Notes` sin `MaximumLength` devuelve 500 en vez de 400 · borrado de documento antes de
      confirmar la base · invalidaciones de caché faltantes tras mutaciones.

---

## Bloque 4 — Producto y UX

Flujos que la interfaz promete y no existen. Ver el detalle en la sección 6 de la auditoría.

- [ ] **No se puede editar un contrato**: el botón existe y no hace nada. El hook está implementado
      y sin usar. → `ContratoDetailPage.tsx:553`
- [ ] **"Ajustes" es sólo un historial**: no hay forma de ver ni aplicar los ajustes pendientes del
      período, que es la tarea recurrente más importante del administrador.
- [ ] **No hay forma de crear débitos ni créditos manuales**, aunque los filtros los ofrecen.
      El descuento por una reparación no se puede registrar.
- [ ] **Al inquilino se le muestra la URL prefirmada cruda**, con token y cronómetro.
- [ ] **El panel muestra tendencias inventadas** junto a datos reales. Es un riesgo de producto: son
      afirmaciones falsas sobre el negocio del cliente. → `DashboardPage.tsx:34-35`
- [ ] **Los seis campos de registrar pago y ajuste manual usan una clase CSS inexistente**
      (`className="inp"`). Arreglo de seis caracteres. → `ContratoDetailPage.tsx:214-329`
- [ ] Accesibilidad: ~28 campos sin etiqueta asociada · cero navegación por teclado en filas
      clickeables · chips de estado por debajo del contraste mínimo.
- [ ] **`pnpm lint` está roto**: 13 errores en `portal-admin/routes.tsx`. El CI no lo corre, así que
      pasa desapercibido.

---

## Cómo priorizar

Si hay que elegir un solo orden, este:

1. **Motor de ajustes** (bloque 1) — sin esto no hay demo que cierre.
2. **La cadena de revocación** (bloque 2: refresh → token corto → logout real → baja efectiva).
3. **División por cero y enums sin validar** (bloque 3) — tocan plata y son baratos.
4. **Editar contrato y ajustes pendientes** (bloque 4) — los dos agujeros de flujo más visibles.
5. **WhatsApp y punitorios** (bloque 1) — el argumento comercial más fuerte disponible.
