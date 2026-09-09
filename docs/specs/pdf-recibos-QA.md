# QA end-to-end — Marca, Rendiciones y Recibos PDF (demo Palavecino)

Fecha: 2026-09-09
Entorno: https://web-production-dc836.up.railway.app (org `palavecino`)
Herramienta: agent-browser (CDP headless)

## Checklist

- [x] **1. Login admin** — Ingreso con org `palavecino` + credenciales de `secretos-prod.local.txt` (ADMIN_EMAIL/ADMIN_PASSWORD). Redirige correctamente a `/admin/dashboard`. **OK.**

- [x] **2. Configuración → Marca carga con datos guardados** — `/admin/configuracion/marca` muestra: NOMBRE COMERCIAL "Palavecino y Asociados", RAZÓN SOCIAL "Palavecino y Asociados S.R.L.", CUIT "30-71234567-9", DOMICILIO "Av. Presidente Perón 1234, Luján", COLOR DE MARCA "#1F4E79". Selector de color presente (`<input type="color">` + campo hex sincronizado) y área de logo visible. **OK.**

- [x] **3. Subida de logo** — Generado un PNG mínimo (64x64, color #1F4E79) por script Python. Subido vía el input de archivo oculto detrás de "Subir logo". Tras subir, aparece vista previa (`<img alt="Logo de la inmobiliaria">` con blob URL) y los botones cambian a "Cambiar logo"/"Quitar". Guardado con "Guardar cambios". **Recargando la página (navegación completa, no solo refresh de estado) el logo sigue presente** — la imagen se vuelve a pedir al servidor (nueva blob URL) y los botones "Cambiar logo"/"Quitar" persisten. **OK.**
  - Evidencia: captura `marca-logo.png` (adjunta).

- [~] **4. Rendiciones — Carlos Fernández, 09/2026 a 09/2026** — Seleccionado propietario "Carlos Fernández", período por defecto ya cargado en 09/2026–09/2026, tocado "Ver". Resultado en tabla:
  - INMUEBLE "agrelo al 1600" — COBRADO `$ 420.000` — COMISIÓN % `8.00%` — COMISIÓN `$ 33.600` — NETO `$ 386.400`.
  - Pie: Total cobrado `$ 420.000`, Comisión de administración `-$ 33.600`, NETO A LIQUIDAR `$ 386.400`.
  - Los importes coinciden en valor y usan separador de miles con punto (formato argentino). **Observación:** no se muestran los dos decimales (`,00`) — el HTML renderiza `$&nbsp;420.000` / `$&nbsp;386.400` sin parte decimal, no `$ 420.000,00` / `$ 386.400,00` como se esperaba verificar. Los montos son correctos, solo falta la coma decimal cuando el resto es .00.
  - Evidencia: captura `rendicion.png` (adjunta).

- [x] **5. Descargar PDF (Rendiciones)** — Al tocar "Descargar PDF" se dispara `GET /api/v1/owners/{id}/settlement/pdf?from=2026-09-01&to=2026-09-01` → `200 application/pdf`, 48.448 bytes, cabecera `%PDF-1.4` verificada (contenido binario válido, verificado con `fetch` en el contexto de la página). **Nota de herramienta:** el comando `agent-browser download` reportó "Download was canceled" en modo headless (limitación conocida de descargas en Chrome headless vía CDP, no del sitio) — se verificó el contenido igualmente por fetch directo, con las mismas credenciales/cookies de sesión que usa el botón. **OK (funcionalmente), con nota de herramienta.**

- [~] **6. Pagos — recibo** — En `/admin/pagos` solo existe **1 transacción** en los datos de la demo, de tipo "Pago" ($ 420.000, Ana López · agrelo al 1600). Esa fila muestra el botón "Descargar recibo" y al tocarlo dispara `GET /api/v1/transactions/{id}/receipt` → `200 application/pdf`, 53.782 bytes. **No se pudo verificar la exclusión ("solo en filas de tipo pago") por ausencia de otras filas (cargos/débitos/créditos) en los datos de esta demo** — comportamiento visual correcto en el único caso disponible, pero regla de exclusión no confirmada end-to-end. Misma nota de herramienta que el punto 5 sobre `agent-browser download`.

- [x] **7. Errores de consola** — `agent-browser console` no registró ningún mensaje (ni error, ni warning, ni log) durante toda la sesión, incluyendo login, navegación, subida de logo, consulta de rendición y descarga de recibo. **Sin errores.**

## Resumen de hallazgos

1. Formato de moneda en Rendiciones no muestra los decimales `,00` (revisar el formatter de moneda usado en esa pantalla).
2. La exclusividad del botón "Descargar recibo" a filas de tipo "Pago" no se pudo probar por falta de datos de otros tipos en la demo — sugerido: cargar una transacción de tipo Cargo/Débito/Crédito de prueba para confirmar.
3. `agent-browser download` no logra materializar el archivo en disco en este entorno headless ("Download was canceled"); se compensó verificando byte a byte el response (content-type, tamaño, cabecera PDF) vía `fetch` con las cookies de sesión reales. Recomendado repetir la descarga manual en un navegador real si se quiere evidencia visual del diálogo de guardado.

## Re-verificación — 2026-09-09 (post-correcciones)

Entorno y herramienta iguales a la corrida anterior (agent-browser CDP). Login OK con org `palavecino`.

- [x] **1. Rendiciones — Carlos Fernández, 09/2026 a 09/2026** — Tabla resultado: INMUEBLE "agrelo al 1600" — COBRADO `$ 420.000,00` — COMISIÓN % `8.00%` — COMISIÓN `$ 33.600,00` — NETO `$ 386.400,00`. Pie: Total cobrado `$ 420.000,00`, Comisión de administración `-$ 33.600,00`, NETO A LIQUIDAR `$ 386.400,00`. Formato argentino confirmado (miles con punto, decimales con coma, ",00" presente). **OK — corregido.**

- [x] **2. Pagos — recibo solo en fila de Pago** — `/admin/pagos` ahora muestra 2 transacciones: fila "Débito manual" ($18.500, Sep 2026, nota "Punitorio por pago fuera de término.") **sin** botón de recibo (celda NOTAS/FECHA sin acción), y fila "Pago" ($420.000, "Pago recibido por transferencia.") **con** botón "Descargar recibo". Balance neto $401.500. Exclusividad confirmada end-to-end. **OK — corregido.**

- [x] **3. Errores de consola** — `agent-browser console` y `agent-browser errors` sin salida (sin errores/warnings) en ambas pantallas (`/admin/rendiciones` y `/admin/pagos`). **Sin errores.**

**Veredicto: LISTO.**
