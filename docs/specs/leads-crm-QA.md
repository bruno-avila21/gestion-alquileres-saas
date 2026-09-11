# QA — CRM de consultas (leads), bloque A3

Fecha: 2026-09-08 · Rama: `fix/audit-2026-07-18` · Entorno: API :5000 (Development), Vite :5173, Postgres/MinIO docker.
Org: `palavecino`, admin `admin@palavecino.demo` (no pidió cambio de contraseña forzado; se usó `Palavecino2026!` sin cambios).

Herramientas: `curl` (API), `agent-browser` (navegación real, snapshots de accesibilidad, consola, screenshots) a 1280×800 y 390×844.

## Resultado por criterio

| # | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 1 | Ficha de propiedad: form "Consultar por esta propiedad", envío nombre+teléfono+mensaje → estado enviado, consola limpia, lead en API con `listingId`/`propertyTitle`/`source=Website` | ✔ pasa | qa/2026-09-08-ficha-consulta-enviada.png · API: lead `b26fa901…` con `listingId=c78b8aef…`, `propertyTitle="Alquiler Local Comercial en Bella Vista"`, `source=Website` |
| 2 | Home: sección Contacto envía consulta general (sin listingId) | ✔ pasa | qa/2026-09-08-home-contacto-enviado.png · API: lead `aa29423c…` con `listingId=null`, `source=Website` |
| 3 | Honeypot: `website` con contenido → 204, no crea lead | ✔ pasa | `curl -i` → `204 No Content`; `GET /leads/summary` total antes=0, después=0 |
| 4 | Validación: sin email ni teléfono → 400 | ✔ pasa | `curl -i` → `400`, body `{"errors":[{"field":"Email","message":"Debe indicar email o teléfono."}]}` |
| 5 | Admin: menú "Consultas"; tablero 6 columnas con conteos; leads en "Nueva" | ✔ pasa | qa/2026-09-08-admin-consultas-tablero.png — columnas Nueva/Contactada/Visita/Negociación/Ganada/Perdida, conteos correctos, 4 leads en Nueva |
| 6 | Drag&drop a "Contactada", persiste al recargar | ✔ pasa (con nota) | qa/2026-09-08-drag-contactada.png — drag nativo funcionó con selector CSS `.kanban-col:nth-child(2) .kanban-col-body`; tras `open` (reload) la tarjeta sigue en Contactada |
| 7 | Mover a "Perdida" pide motivo; sin motivo no deja; con motivo persiste | ✔ pasa (con nota sobre método) | qa/2026-09-08-motivo-modal-vacio.png, qa/2026-09-08-perdida-confirmada.png — botón "Marcar como perdida" queda deshabilitado sin texto en MOTIVO y se habilita al completarlo; probado vía el selector ESTADO del drawer (ver nota abajo sobre drag a Perdida) |
| 8 | Drawer: agregar nota (aparece en timeline, sube contador), editar nombre, eliminar con confirmación | ✔ pasa | qa/2026-09-08-nota-agregada.png (badge pasó a 1), qa/2026-09-08-nombre-editado.png (nombre cambiado en tarjeta al instante), qa/2026-09-08-eliminar-confirmado.png (`alertdialog` "Eliminar consulta" con Cancelar/Eliminar; tras confirmar, tarjeta desaparece y conteo baja) |
| 9 | "Nueva consulta" manual con publicación elegida → `source=Manual` | ✔ pasa | qa/2026-09-08-nueva-consulta-manual.png · API: lead `8e82b75c…` con `source="Manual"`, `listingId` de la propiedad elegida |
| 10 | Buscador filtra por nombre | ✔ pasa | qa/2026-09-08-buscador-filtrado.png — con "Juan QA" solo queda la tarjeta de "Juan QA Home" en Contactada; el resto muestra "Sin consultas" (el conteo del encabezado no cambia, viene de `/summary`, tal como documenta el informe web) |
| 11 | Consola limpia en admin y público | ✔ pasa | Sin errores en ninguna de las capturas de `agent-browser console` durante todo el flujo (público y admin). Solo logs de Vite HMR/React DevTools (ruido normal de dev) |
| 12 | Responsive 390px: tablero y form público sin scroll horizontal de página (solo dentro del tablero) | ✔ pasa | qa/2026-09-08-mobile-admin-consultas.png (`document.body.scrollWidth === innerWidth === 390`, el kanban scrollea internamente con flechas propias) y qa/2026-09-08-mobile-ficha-form.png (form apilado, inputs full-width, WhatsApp FAB visible) |

**12/12 criterios pasan.**

## Notas de método (no son fallos del CRM)

- **Drag & drop en el paso 7 no funcionó con `agent-browser drag`** cuando la columna "Perdida" estaba fuera del viewport (6ª de 6, requiere scroll horizontal del tablero): el primer intento con selector `nth-child(6)` no movió nada (coordenadas resueltas antes del scroll), y un segundo intento tras hacer `scrollLeft` con `eval` terminó seleccionando texto en pantalla en lugar de arrastrar (mismatch de coordenadas por el scroll). Se cambió al selector accesible del drawer (`combobox ESTADO` → opción "Perdida"), documentado en el propio contrato como alternativa válida ("si Playwright no lo logra, usar el selector del detalle"). El paso 6 (Contactada, 2ª columna, visible sin scroll) sí funcionó con drag nativo por CSS selector sin problema.
- El honeypot se verificó en el DOM: el input vive en un `div.visually-hidden` con `position:absolute; clip:rect(0,0,0,0); width:1px` — no usa `display:none`, tal como pide el contrato.

## Hallazgo fuera de alcance (no bloquea este bloque)

- **Media (fuera de A3):** en el sitio público (home y resultados de búsqueda), los links "Ver ficha de {propiedad}" sobre las tarjetas no navegan al hacer click con automatización (`agent-browser click` sobre el `<a>` no cambia la URL); navegar por URL directa (`/sitio/palavecino/propiedades/{id}`) sí funciona y ahí el formulario de consulta funciona perfectamente. No se investigó la causa (posible listener que hace `preventDefault` o captura del click en un elemento superpuesto) porque es una página que no forma parte del bloque A3 (leads). Se recomienda que QA de la propia ficha/listado lo re-verifique con un click real de mouse (no automatizado) antes de asumir que es el mismo problema.

## Veredicto

**LISTO.** Los 12 criterios del bloque A3 pasan con evidencia (capturas + verificación cruzada API/UI). No se encontraron hallazgos altos dentro del alcance del CRM de leads. Lo primero que haría: revisar el link "Ver ficha" del sitio público (hallazgo fuera de alcance) porque afecta la llegada de usuarios reales a la propia página donde vive el formulario de consulta del paso 1.

## Re-verificación del hallazgo "Ver ficha" (a pedido del coordinador)

Se probó en http://localhost:5173/sitio/palavecino, tarjeta "Alquiler Local Comercial en Bella Vista" (1280×800):

**(a) Click de mouse real por coordenadas** (`mouse move` → `mouse down` → `mouse up` en el centro de la tarjeta, tras `scrollIntoView`): **navegó correctamente** a `/sitio/palavecino/propiedades/c78b8aef-280f-4f31-8353-8a71f1be0a9e`, sin errores de consola. No hizo falta probar (b) ni (c).

**Conclusión:** confirmado, era un falso positivo del método de automatización (`agent-browser click @ref`, que dispara el evento de forma distinta a un click de mouse real —probablemente por el `translateY(-4px)` del hover que corre la tarjeta entre el `pointerdown` sintético y el `click`). El `<Link>` de react-router en `ListingCard.tsx` funciona bien. **Se retira el hallazgo** de la sección "fuera de alcance"; no hay ningún bug en la navegación del sitio público. El veredicto LISTO del bloque A3 no cambia (este punto nunca fue parte de los 12 criterios evaluados).
