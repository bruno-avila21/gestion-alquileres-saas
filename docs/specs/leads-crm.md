# Spec — CRM de consultas (leads) · bloque A3

Contrato compartido entre API y web. Ambos agentes construyen contra esto; no lo cambien sin avisar.

## Dominio

**Lead** (tabla `leads`, multi-tenant por `OrganizationId`, filtro global EF):
- `Id` Guid · `OrganizationId` Guid
- `ListingId` Guid? · `PropertyId` Guid? (se resuelve desde el listing al crear; puede venir null en consulta general)
- `Name` string(120) req · `Email` string(200)? · `Phone` string(40)? — al menos uno de los dos
- `Message` string(2000) req
- `Source` enum `LeadSource { Website = 0, Manual = 1 }`
- `Status` enum `LeadStatus { New = 0, Contacted = 1, Visit = 2, Negotiation = 3, Won = 4, Lost = 5 }`
- `LostReason` string(300)?
- `CreatedAt`, `UpdatedAt`, `LastContactAt`? (se setea al cambiar de estado o agregar nota)

**LeadNote** (tabla `lead_notes`, multi-tenant):
- `Id`, `LeadId`, `OrganizationId`, `Text` string(2000) req, `CreatedByUserId` Guid, `CreatedByName` string(200), `CreatedAt`

## DTOs (JSON camelCase)

```
LeadDto        { id, name, email, phone, message, source, status, lostReason, listingId, propertyId,
                 propertyTitle?, propertyAddress?, listingOperation?, createdAt, updatedAt, lastContactAt, notesCount }
LeadDetailDto  = LeadDto + { notes: LeadNoteDto[] }   // notas ordenadas desc por createdAt
LeadNoteDto    { id, text, createdByName, createdAt }
LeadSummaryDto { total, byStatus: { "New": n, "Contacted": n, ... } }   // claves = nombre del enum
```
Enums se serializan como string (igual que el resto de la API).

## Endpoints

Público (anónimo, resuelto por slug como `/public/{slug}` existente):
- `POST /api/v1/public/{slug}/leads`
  body `{ name, email?, phone?, message, listingId?, website? }`
  - `website` es honeypot: si viene con contenido → 204 sin crear nada (no revelar).
  - valida: name req, message req, email o phone req, listingId (si viene) debe pertenecer a la org y estar Published.
  - rate limit: policy `"public-leads"` 10 req/min por IP → 429.
  - 201 `{ id }`.

Admin (JWT, OrganizationId del token):
- `GET  /api/v1/leads?status=&search=&page=1&pageSize=50` → paged `{ items: LeadDto[], total, page, pageSize }` (mismo envelope paginado que use el resto de la API; si no hay uno estándar, este).
  `search` matchea name/email/phone/propertyTitle (ILIKE).
- `GET  /api/v1/leads/summary` → `LeadSummaryDto`
- `GET  /api/v1/leads/{id}` → `LeadDetailDto` (404 si no es de la org)
- `POST /api/v1/leads` body `{ name, email?, phone?, message, listingId? }` → 201 LeadDto (Source = Manual)
- `PUT  /api/v1/leads/{id}` body `{ name, email?, phone?, message }` → 200 LeadDto
- `PATCH /api/v1/leads/{id}/status` body `{ status, lostReason? }` → 200 LeadDto (lostReason obligatorio si status = Lost; setea LastContactAt)
- `POST /api/v1/leads/{id}/notes` body `{ text }` → 201 LeadNoteDto (setea LastContactAt)
- `DELETE /api/v1/leads/{id}` → 204

## Web

Público (`web/src/portal-publico`):
- En `FichaPage`: bloque "Consultar por esta propiedad" con nombre, email, teléfono, mensaje, honeypot oculto (`website`, `autocomplete=off`, fuera de la vista con CSS, no `display:none`). Estados: enviando / enviado ("Te contactamos a la brevedad") / error. Envía `listingId`.
- En `HomePage`: sección "Contacto" con el mismo formulario sin `listingId`.
- Servicio en `features/public` usando `publicApi` (sin auth).

Admin (`web/src/portal-admin`, ruta `/admin/consultas`, ítem de menú "Consultas"):
- Tablero Kanban con 6 columnas en el orden del enum. Etiquetas: Nueva · Contactada · Visita · Negociación · Ganada · Perdida. Encabezado de columna con el conteo de `summary`.
- Tarjeta: nombre, propiedad (título corto) o "Consulta general", canal (email/teléfono), hace cuánto (createdAt), badge de notas.
- Mover de estado: arrastrar y soltar (HTML5 DnD nativo) **y** un selector en el detalle como alternativa accesible. Si el destino es Perdida, pedir motivo (modal chico).
- Buscador por texto (usa `search`).
- Detalle en drawer/panel lateral: datos de contacto con `mailto:`/`tel:`, mensaje completo, link a la ficha admin de la propiedad, línea de tiempo de notas + textarea para agregar nota, edición de datos, eliminar con confirmación.
- Botón "Nueva consulta" (carga manual) con selector de publicación opcional.
- Estados vacío / cargando / error en cada columna y en el drawer.

## Fuera de alcance (no hacer ahora)
Asignación a usuarios, recordatorios, emails automáticos al lead, importación desde Tokko.
