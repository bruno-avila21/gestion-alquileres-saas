# Backend — Recibos y liquidaciones en PDF (informe)

Estado: **LISTO** en el alcance backend (partes A, B, C + Infraestructura de
`docs/specs/pdf-recibos-liquidaciones.md`). El frontend lo construyó otro agente en paralelo
(`docs/specs/pdf-recibos-web-informe.md`), contra los mismos endpoints/DTOs descritos acá.

## Resultado de verificación

- `dotnet build` → limpio, 0 advertencias, 0 errores.
- `dotnet test` → 330 tests, **329 en verde**, 1 rojo (`StorageProviderValidationTests.Production_with_s3_storage_passes`,
  preexistente y no relacionado — falla por una regla de `SecuritySettingsValidator` sobre
  `Registration:Mode`/`AllowedHosts`, nada que ver con este bloque).
- Los dos PDF de muestra (recibo y liquidación, con marca y color propio) se generaron con el
  generador real de QuestPDF (no un fake) en
  `tests/GestionAlquileres.Tests/Phase13/Application/PdfGenerationSampleTests.cs`, verificando
  automáticamente que los bytes empiezan con `%PDF` y pesan más de 1 KB, y quedaron guardados en
  `api/artifacts-qa/recibo-muestra.pdf` (50.5 KB) y `api/artifacts-qa/liquidacion-muestra.pdf`
  (45.9 KB). Confirmado con `file`: `PDF document, version 1.4, 1 page(s)`.

## Migración

`20260909050611_AddOrganizationBrandingAndReceiptNumber` — agrega `legal_name`, `tax_id`, `address`,
`phone`, `email`, `logo_storage_key`, `brand_color`, `receipt_sequence` a `organizations`, y
`receipt_number` a `transactions` con índice único filtrado `(organization_id, receipt_number)
WHERE receipt_number IS NOT NULL`.

## Endpoints nuevos

| Método | Ruta | Auth | Devuelve |
|---|---|---|---|
| GET | `/api/v1/organization` | Admin/Staff | `OrganizationDto` |
| PUT | `/api/v1/organization` | Admin/Staff | `OrganizationDto` |
| POST | `/api/v1/organization/logo` | Admin/Staff | `OrganizationDto` |
| DELETE | `/api/v1/organization/logo` | Admin/Staff | 204 |
| GET | `/api/v1/organization/logo` | Admin/Staff | bytes del logo, o 404 |
| GET | `/api/v1/transactions/{id}/receipt` | Admin/Staff | PDF (`application/pdf`) o 404/409 |
| GET | `/api/v1/owners/{ownerId}/settlement/pdf?from=&to=` | Admin/Staff | PDF o 404/409 |

## Piezas nuevas

- `Domain/Reports/AmountInWords.cs` — conversor a letras (español rioplatense), 22 tests cubriendo
  todos los casos exigidos por el contrato más los bordes de rango.
- `Domain/Reports/ReportModels.cs` — `AgencyBrand`, `ReceiptReport`, `OwnerSettlementReport(+Line)`.
- `Domain/Interfaces/Services/IPdfReportGenerator.cs` + `Infrastructure/Reports/QuestPdfReportGenerator.cs`
  (singleton, QuestPDF 2026.8.0, licencia Community).
- `IOrganizationRepository.IncrementReceiptSequenceAsync` — incremento atómico con `ExecuteUpdateAsync`
  dentro de una transacción explícita (ver decisión de tests más abajo).

## Decisiones propias / cabos sueltos

1. **`ExecuteUpdateAsync` no funciona bajo el proveedor InMemory de la suite de tests.** Lo comprobé
   con un programa de prueba antes de escribir el handler: tanto `ExecuteUpdateAsync` como
   `Database.BeginTransactionAsync()` lanzan excepción bajo `UseInMemoryDatabase` (no traduce la
   query / no soporta transacciones). Como la regla del bloque es no negociable, implementé
   `OrganizationRepository.IncrementReceiptSequenceAsync` con el camino atómico real
   (`BeginTransactionAsync` + `ExecuteUpdateAsync` + re-read + commit) sólo cuando
   `_db.Database.IsRelational()` es true (Postgres real); si no (InMemory, sólo en tests), cae a un
   camino no atómico (load + increment + `SaveChangesAsync`) — no hay concurrencia real que proteger
   ahí. Está documentado en el propio código. Los tests de numeración (mismo número al repetir
   pedido, números consecutivos entre transacciones) corren igual — tanto a nivel handler con fakes
   como end-to-end por HTTP contra el endpoint real — porque ese camino no-atómico basta para probar
   la *regla de negocio* del handler; la garantía de atomicidad frente a concurrencia real sólo la
   da el camino relacional, que no se puede ejercitar sin un Postgres real (fuera del alcance de la
   suite hermética actual, ver nota de AGENTS.md sobre tests sin Postgres).
2. **`Organization` no tiene campo de mime type para el logo.** El contrato de la parte A sólo define
   `LogoStorageKey`. `LocalFileStorageService`/`S3StorageService.UploadAsync` ya conservan la
   extensión original al generar la clave, así que `GET /organization/logo` infiere el
   `Content-Type` (`image/png` / `image/jpeg` / `image/webp`) a partir de la extensión de
   `LogoStorageKey` en vez de agregar una columna nueva no pedida por el spec.
3. **Namespace `Features/Organizations` (plural) en Application**, no `Features/Organization`:
   usar el singular colisiona con el tipo `Domain.Entities.Organization` (CS0118, "es un namespace
   pero se usa como un tipo") apenas se agrega `using GestionAlquileres.Domain.Entities;` en el mismo
   archivo. Sigue además la convención ya usada por Owners/Properties/Contracts (carpeta en plural,
   entidad en singular). El controller queda `OrganizationController` con ruta `/api/v1/organization`
   (singular), tal cual pide el contrato — sólo cambia el nombre de carpeta en Application.
4. **Validación de logo (tipo/tamaño) va por `BusinessException` (409), no por `FluentValidation`
   (400)**, tal cual lo pide el contrato explícitamente — a diferencia del validador de fotos de
   propiedad ya existente en el repo (`UploadPropertyPhotoCommandValidator`), que sí usa
   FluentValidation para lo mismo. Es una asimetría real entre este bloque y el patrón previo del
   repo, no un descuido.
5. **`GetOwnerSettlementPdfQuery` no reusa `GetOwnerSettlementQueryHandler.Handle` tal cual** porque
   ese handler responde 409 ("Propietario no encontrado.") ante un propietario inexistente, y el
   contrato de la variante PDF pide 404 (GET → sólo 404). Extraje dos métodos estáticos compartidos
   (`ValidatePeriod`, `BuildDto`) del handler JSON para no duplicar el cálculo de comisión/totales,
   y el handler del PDF hace su propio chequeo de existencia antes de llamarlos.
6. **Nombre de archivo de la liquidación** (`liquidacion-{apellido-o-slug}-{yyyyMM}-{yyyyMM}.pdf`):
   `Owner` sólo tiene un campo `Name` (puede ser una razón social), así que en vez de separar un
   apellido inexistente, slugifico el nombre completo del propietario (minúsculas, sin acentos,
   espacios → guiones).
7. **`AgencyBrandFactory`** (`Application/Common/Reports/`) centraliza "armar `AgencyBrand` +
   bajar el logo del storage", compartido entre el recibo y la liquidación — evita duplicar esa
   lógica en los dos handlers de PDF.

## Cómo probarlo manualmente

1. `POST /api/v1/contracts/{id}/payments` para generar una transacción de pago.
2. `GET /api/v1/transactions/{id}/receipt` descarga el PDF (nombre `recibo-REC-00000001.pdf` la
   primera vez; pedirlo de nuevo devuelve el mismo número).
3. `PUT /api/v1/organization` con `brandColor` en `#RRGGBB` y `POST /api/v1/organization/logo`
   (PNG/JPG/WebP ≤ 2 MB) para ver la marca en el encabezado del próximo PDF.
4. `GET /api/v1/owners/{ownerId}/settlement/pdf?from=2026-01-01&to=2026-03-01` para la liquidación.
