# Informe — CRM de consultas (leads), frontend (bloque A3)

Implementado contra `docs/specs/leads-crm.md`. El backend lo construye otro agente en paralelo
contra el mismo contrato: todo lo de acá está programado contra los DTOs/rutas del spec, no
contra la API viva (no había servidor con `/leads` corriendo al momento de implementar).

## Archivos creados

### Público (`features/public` + `portal-publico`)
- `web/src/features/public/types/public.types.ts` — agregado `CreatePublicLeadRequest`.
- `web/src/features/public/services/publicService.ts` — agregado `createLead(slug, body)` →
  `POST /public/{slug}/leads`. Trata 201 y 204 (honeypot) igual: éxito, sin revelar el descarte.
- `web/src/features/public/hooks/usePublic.ts` — agregado `useCreatePublicLead(slug)`.
- `web/src/features/public/components/LeadForm.tsx` — formulario reusado en FichaPage (con
  `listingId`) y HomePage (sin `listingId`). Honeypot `website` envuelto en la clase
  `.visually-hidden` ya existente en `publico.css` (clip + posición absoluta, no `display:none`).
  Validación cliente: nombre y mensaje requeridos, email o teléfono (al menos uno). Estados
  idle/sending/sent/error.
- `web/src/portal-publico/pages/FichaPage.tsx` — agrega bloque "Consultar por esta propiedad"
  (`.lead-card`) debajo de la tarjeta de contacto, con `listingId={p.id}`.
- `web/src/portal-publico/pages/HomePage.tsx` — agrega sección `#contacto` con el mismo `LeadForm`
  sin `listingId`.
- `web/src/portal-publico/publico.css` — clases `.lead-card`, `.lead-form` y variantes (`.lf-field`,
  `.lf-err`, `.lf-submit`, `.lf-status.ok/.error`), con la paleta ladrillo/navy existente.

### Admin (`features/leads` + `portal-admin`)
- `web/src/features/leads/types/lead.types.ts` — `LeadStatus`, `LeadSource`, `LeadDto`,
  `LeadNoteDto`, `LeadDetailDto`, `LeadSummaryDto` (`byStatus` tipado `Partial<Record<...>>` por
  si la API omite un estado sin consultas), `PagedResult<T>`, requests de create/update/status/nota.
- `web/src/features/leads/services/leadService.ts` — `list`, `summary`, `getById`, `create`,
  `update`, `updateStatus`, `addNote`, `remove` contra `/api/v1/leads*`.
- `web/src/features/leads/hooks/useLeads.ts` — `useLeads`, `useLeadSummary`, `useLead`,
  `useCreateLead`, `useUpdateLead`, `useDeleteLead`, `useAddLeadNote`, `useUpdateLeadStatus`.
  Tres raíces de query key separadas (`['leads','list']`, `['leads','summary']`,
  `['leads','detail']`) para que el `setQueriesData` optimista de `useUpdateLeadStatus` no pise por
  accidente la caché de detalle (forma distinta a la de lista). `useUpdateLeadStatus` hace
  optimistic update en lista + summary + detalle, con rollback exacto en `onError` y
  revalidación en `onSettled`.
- `web/src/features/leads/hooks/useListingOptions.ts` — `GET /listings` sin `propertyId` (el
  parámetro ya es opcional en la API existente) para el selector de "Nueva consulta".
- `web/src/features/leads/utils/timeAgo.ts` — "hace 5 min/h/d/sem" para la tarjeta del Kanban.
- `web/src/features/leads/components/`:
  - `LeadCard.tsx` — tarjeta draggable (HTML5 DnD nativo).
  - `LeadColumn.tsx` — columna con drop target, estados vacío/cargando/error propios.
  - `LeadKanbanBoard.tsx` — arma las 6 columnas, agrupa por estado, dispara
    `useUpdateLeadStatus` en el drop (abre `LostReasonModal` si el destino es "Perdida").
  - `LostReasonModal.tsx` — motivo obligatorio al mover a Perdida (drag o selector del drawer).
  - `ListingPicker.tsx` — buscador simple client-side sobre `useListingOptions` (título/dirección/código).
  - `LeadFormModal.tsx` — "Nueva consulta" (alta manual, `Source=Manual`).
  - `LeadDetailDrawer.tsx` — drawer lateral: selector de estado accesible (alternativa a drag&drop),
    contacto `mailto:`/`tel:`, link a la ficha admin de la propiedad, mensaje + edición inline,
    timeline de notas + alta de nota, eliminar con `ConfirmDialog`.
- `web/src/portal-admin/pages/ConsultasPage.tsx` — página con buscador, tablero y botón
  "Nueva consulta".
- `web/src/portal-admin/routes.tsx` — ruta `consultas` → `ConsultasPage`.
- `web/src/portal-admin/layouts/AdminSidebar.tsx` — ítem de menú "Consultas" (icono `IcMail`).
- `web/src/index.css` — clases nuevas para el Kanban (`.kanban*`), la tarjeta (`.lead-card-item`,
  `.lci-*`), el drawer (`.lead-drawer*`) y el picker (`.listing-picker-*`).

### Deviación menor documentada
- `web/src/portal-admin/pages/PropiedadesPage.tsx` — el spec pide "link a la ficha admin de la
  propiedad" desde el drawer de una consulta, pero **no existe una ruta `/admin/propiedades/:id`**
  (la ficha se abre inline con estado `openFichaId` en la misma página de listado). Se agregó
  soporte mínimo a `?highlight={propertyId}` (leído una sola vez, como estado inicial vía
  `useSearchParams`) para que el link del drawer (`/admin/propiedades?highlight=...`) abra la
  ficha correcta al llegar. Límite conocido: la lista es paginada en el cliente (20/página); si la
  propiedad no cae en la página 0 no se verá abierta hasta que el usuario la busque. No se tocó
  nada más de esa página.

## Rutas nuevas

| Ruta | Portal | Componente |
|------|--------|------------|
| `/admin/consultas` | Admin | `ConsultasPage` |
| — | Público | `LeadForm` embebido en `FichaPage` y `HomePage` (no es ruta nueva, `ContactoPage` existente queda igual) |

## Decisiones de contrato / lo que falta verificar contra la API viva

1. **Kanban carga todo el board de una** (`useLeads({ search, page: 1, pageSize: 200 })`) y agrupa
   por `status` en el cliente — el spec no define paginación por columna. El encabezado de cada
   columna usa el conteo real de `GET /leads/summary` (no afectado por el buscador); las tarjetas sí
   se filtran por `search`. Con más de ~200 consultas activas habría que paginar por columna o subir
   `pageSize`; no se implementó porque el spec no lo pide.
2. `LeadSummaryDto.byStatus` se tipó `Partial<Record<LeadStatus, number>>` (no todas las claves
   garantizadas) — confirmar si la API siempre manda las 6 claves; si es así se puede endurecer el
   tipo.
3. `POST /public/{slug}/leads`: se asume 201 con `{ id }` en éxito real y 204 sin cuerpo en el caso
   honeypot, tal cual el spec. El servicio ignora el body en ambos casos.
4. El selector de publicación de "Nueva consulta" reutiliza `GET /listings` (sin `propertyId`, ya
   opcional en el controller existente) y filtra client-side por título/dirección/código — no hay
   endpoint de búsqueda server-side de listings en el spec, así que esto no depende de nada nuevo
   que tenga que construir el otro agente.
5. Pendiente de correr contra la API real una vez esté levantada: nombres exactos de query params
   (`status`, `search`, `page`, `pageSize`), forma exacta del paged envelope, y que
   `PATCH /leads/{id}/status` acepte `lostReason` solo cuando `status="Lost"` (el cliente ya lo
   fuerza a `null` en cualquier otro estado).

## Verificación

- `pnpm build`: verde (tsc -b && vite build, sin errores).
- `pnpm lint`: 14 errores, los mismos ~14 preexistentes de `portal-admin/routes.tsx`
  (`react-refresh/only-export-components`, falso positivo del archivo por mezclar `lazy()` locales
  con el export de `adminRoutes`). Se agregó una ruta más siguiendo el mismo patrón que las demás,
  con un `eslint-disable-next-line` puntual documentado en esa única línea para no sumar un 15º
  error — no se tocaron ni corrigieron los 14 preexistentes.
- No hay `any` ni `@ts-ignore` en el código nuevo (verificado con grep).
- No se probó manualmente contra Vite (`:5173`) por decisión de alcance del informe (no se
  reinició el server); recomendado un `qa-e2e` o smoke manual una vez el backend de leads esté
  arriba, sobre todo: drag & drop entre las 6 columnas, el modal de motivo al mover a Perdida, y el
  honeypot del formulario público.
