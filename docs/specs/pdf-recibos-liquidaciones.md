# Recibos y liquidaciones en PDF — contrato

**Bloque 1 de PENDIENTES.md.** Estado: en implementación (2026-09-09).

El entregable físico del negocio. Hoy la inmobiliaria arma el recibo en Word. Sin esto, la demo
pierde frente a la competencia. Incluye la **marca de la inmobiliaria**, que hoy no existe en el
modelo: `Organization` sólo tiene `Name`, `Slug`, `Plan`, `IsActive`.

Alcance cerrado en tres partes: **A** marca, **B** recibo de pago, **C** liquidación al propietario.

---

## Decisiones tomadas

| Decisión | Por qué |
|---|---|
| **QuestPDF** (licencia Community) | Gratis con facturación anual bajo USD 1.000.000, uso comercial permitido, sin clave de licencia. API declarativa en C#, sin navegador headless en el contenedor. Hay que declarar `QuestPDF.Settings.License = LicenseType.Community` al arrancar o tira excepción. |
| Generación **en Infrastructure**, no en Application | Application no puede referenciar librerías de terceros (regla de capas). Application arma un modelo de dominio y llama a una interfaz. |
| El PDF **no se persiste** | Se genera en cada descarga desde datos vivos. Un recibo guardado se desincroniza del pago si se corrige el monto. El storage queda para documentos que sube el usuario. |
| **Numeración secuencial persistida** por organización | Un recibo con número que cambia entre descargas no sirve como comprobante. El número se asigna la primera vez y queda en la transacción. |
| Leyenda **"Documento no válido como factura"** | Un recibo de pago de alquiler no es comprobante fiscal. Imprimirlo sin la leyenda induce a error. |

---

## Parte A — Marca de la inmobiliaria

### Campos nuevos en `Organization`

| Campo | Tipo | Notas |
|---|---|---|
| `LegalName` | `string?` | Razón social, si difiere del nombre comercial. Máx. 200. |
| `TaxId` | `string?` | CUIT. Máx. 20. Se guarda tal cual lo escriben. |
| `Address` | `string?` | Domicilio de la inmobiliaria. Máx. 300. |
| `Phone` | `string?` | Máx. 50. |
| `Email` | `string?` | Máx. 200. Formato de email si viene. |
| `LogoStorageKey` | `string?` | Clave en el storage (S3/MinIO). Máx. 200. |
| `BrandColor` | `string?` | Hex `#RRGGBB`. Encabezado y totales del PDF. Si es `null`, gris neutro. |
| `ReceiptSequence` | `long` | Contador de recibos. Arranca en `0`. Ver parte B. |

Migración: `AddOrganizationBranding`.

`Organization` **no** es `ITenantEntity` y no tiene filtro global. Todo acceso se resuelve por el
`OrganizationId` del JWT, nunca por un id del body ni de la ruta.

### Endpoints (`OrganizationController`, `AdminControllerBase`)

| Verbo | Ruta | Cuerpo | Devuelve |
|---|---|---|---|
| GET | `/api/v1/organization` | — | `OrganizationDto` |
| PUT | `/api/v1/organization` | `UpdateOrganizationRequest` | `OrganizationDto` |
| POST | `/api/v1/organization/logo` | multipart, campo `file` | `OrganizationDto` |
| DELETE | `/api/v1/organization/logo` | — | `204` |
| GET | `/api/v1/organization/logo` | — | bytes del logo, o `404` |

`OrganizationDto(Id, Name, LegalName, TaxId, Address, Phone, Email, BrandColor, HasLogo, Plan)`.
No expone `LogoStorageKey`: el frontend pide el logo por `GET /organization/logo`.

`UpdateOrganizationRequest(Name, LegalName, TaxId, Address, Phone, Email, BrandColor)`.

**Validación** (`UpdateOrganizationCommandValidator`):
- `Name` obligatorio, 2–200.
- `BrandColor`, si viene, matchea `^#[0-9A-Fa-f]{6}$`.
- `Email`, si viene, formato de email.
- Longitudes máximas de la tabla de arriba.

**Logo**: máx. **2 MB**, `image/png`, `image/jpeg` o `image/webp`. Otro tipo o tamaño →
`BusinessException`. Al reemplazar el logo se borra el anterior del storage. `DELETE` borra el
archivo y pone `LogoStorageKey = null`.

---

## Parte B — Recibo de pago en PDF

### Campo nuevo en `Transaction`

| Campo | Tipo | Notas |
|---|---|---|
| `ReceiptNumber` | `string?` | `null` hasta la primera descarga del recibo. Índice único por `(OrganizationId, ReceiptNumber)` filtrado a los no nulos. |

Migración: `AddTransactionReceiptNumber` (puede ir junta con la de la parte A).

### Numeración

Formato: `REC-` + 8 dígitos con ceros a la izquierda. Ejemplo `REC-00000042`.

Asignación, **sólo la primera vez** que se pide el recibo de esa transacción:

1. Si la transacción ya tiene `ReceiptNumber`, se usa ese. Fin.
2. Si no: abrir transacción de base, incrementar `Organization.ReceiptSequence` de forma atómica
   (`ExecuteUpdateAsync` con `SetProperty(o => o.ReceiptSequence, o => o.ReceiptSequence + 1)`),
   releer el valor **dentro de la misma transacción**, escribirlo en `Transaction.ReceiptNumber`,
   confirmar.

El bloqueo de fila de Postgres serializa dos pedidos concurrentes: el segundo espera y lee el valor
ya incrementado. No se puede saltear un número ni repetirlo.

### Regla de negocio

Sólo las transacciones de tipo **`Payment`** tienen recibo. Un recibo acredita dinero recibido; un
cargo (`RentCharge`) todavía no se cobró.

- Transacción inexistente → `404` (convención: sólo el GET devuelve 404).
- Existe pero no es `Payment` → `BusinessException` → `409`, mensaje
  `"Sólo se emite recibo de las transacciones de tipo pago."`

### Contenido del recibo

```
┌──────────────────────────────────────────────────────────┐
│ [logo]  NOMBRE INMOBILIARIA            RECIBO N° REC-...  │
│         Razón social · CUIT                Fecha: dd/mm/aa│
│         Domicilio · Tel · Email                           │
├──────────────────────────────────────────────────────────┤
│ Recibí de:      Nombre del inquilino (DNI ...)            │
│ Por el inmueble: Dirección, barrio, ciudad                │
│ En concepto de: Alquiler período mm/aaaa                  │
├──────────────────────────────────────────────────────────┤
│ SON: Pesos ciento veinte mil con 00/100                   │
│                                        $ 120.000,00       │
├──────────────────────────────────────────────────────────┤
│ Observaciones (si la transacción tiene Notes)             │
│                                                           │
│                              ______________________       │
│                                 Firma y sello             │
│ Documento no válido como factura.                         │
└──────────────────────────────────────────────────────────┘
```

- Fecha del recibo: `PaidAt` si existe, si no `CreatedAt`, en zona **America/Argentina/Buenos_Aires**.
- Moneda: `$` para ARS, `USD` para dólares. El importe en letras nombra la moneda
  (`Pesos …` / `Dólares estadounidenses …`).
- Si `Organization.LogoStorageKey` es `null`, el encabezado va sin logo, sólo texto.

### Importe en letras

`Domain/Reports/AmountInWords.cs`, función pura, sin dependencias, **con tests**.

- Español rioplatense. `1` → `un`, `21` → `veintiún`, `100` → `cien`, `101` → `ciento uno`,
  `1.000.000` → `un millón`, `2.000.000` → `dos millones`.
- Centavos como fracción: `con 00/100`, `con 50/100`.
- Rango soportado: `0` a `999.999.999,99`. Fuera de rango → excepción.
- Casos que los tests tienen que cubrir sí o sí: `0`, `1`, `15`, `16`, `21`, `30`, `100`, `101`,
  `200`, `500`, `900`, `1000`, `1001`, `21000`, `100000`, `1000000`, `2000000`, `999999999.99`,
  y decimales `0.05`, `1234.5`.

### Endpoint

`GET /api/v1/transactions/{id}/receipt` → `application/pdf`,
`Content-Disposition: attachment; filename="recibo-REC-00000042.pdf"`.

Query: `GetPaymentReceiptPdfQuery(Guid TransactionId)` → `PdfFileDto(byte[] Content, string FileName)`.

---

## Parte C — Liquidación al propietario en PDF

Reusa `GetOwnerSettlementQuery`, que ya calcula todo. El PDF sólo formatea.

### Contenido

```
┌──────────────────────────────────────────────────────────┐
│ [logo]  NOMBRE INMOBILIARIA        LIQUIDACIÓN AL         │
│         CUIT · Domicilio · Tel        PROPIETARIO         │
│                                    Período mm/aaaa–mm/aaaa│
├──────────────────────────────────────────────────────────┤
│ Propietario: Nombre (CUIT ...)                            │
├──────────────────────────────────────────────────────────┤
│ Inmueble          Cobrado    Comisión %   Comisión  Neto  │
│ ...                                                       │
├──────────────────────────────────────────────────────────┤
│ Total cobrado                                  $ ...      │
│ Comisión de administración                    -$ ...      │
│ NETO A LIQUIDAR                                $ ...      │
├──────────────────────────────────────────────────────────┤
│ CBU para la transferencia: ... (si el propietario tiene)  │
│ Documento no válido como factura.                         │
└──────────────────────────────────────────────────────────┘
```

- Si no hay líneas (ningún cobro en el período), el PDF se emite igual con la tabla vacía y la
  leyenda `Sin cobranzas registradas en el período.` No es un error.
- Encabezado de tabla repetido en cada página; totales en la última.

### Endpoint

`GET /api/v1/owners/{ownerId}/settlement/pdf?from=&to=` → `application/pdf`,
`filename="liquidacion-{apellido-o-slug}-{yyyyMM}-{yyyyMM}.pdf"`.

Query: `GetOwnerSettlementPdfQuery(Guid OwnerId, DateOnly From, DateOnly To)` → `PdfFileDto`.

Propietario inexistente → `404`. `to < from` → `409` (ya lo valida el handler existente).

---

## Contratos internos

`Domain/Reports/`:

```csharp
public record AgencyBrand(
    string Name, string? LegalName, string? TaxId, string? Address,
    string? Phone, string? Email, byte[]? Logo, string? BrandColor);

public record ReceiptReport(
    string Number, DateOnly IssuedOn, AgencyBrand Agency,
    string PayerName, string? PayerDocument, string PropertyAddress,
    string Concept, decimal Amount, string CurrencyCode, string AmountInWords, string? Notes);

public record OwnerSettlementReport(
    AgencyBrand Agency, string OwnerName, string? OwnerTaxId, string? OwnerCbu,
    DateOnly PeriodFrom, DateOnly PeriodTo,
    decimal GrossCollected, decimal Commission, decimal NetToOwner,
    IReadOnlyList<OwnerSettlementReportLine> Lines);

public record OwnerSettlementReportLine(
    string PropertyAddress, decimal Collected, decimal CommissionPct,
    decimal Commission, decimal Net);
```

`Domain/Interfaces/Services/IPdfReportGenerator.cs`:

```csharp
public interface IPdfReportGenerator
{
    byte[] RenderReceipt(ReceiptReport report);
    byte[] RenderOwnerSettlement(OwnerSettlementReport report);
}
```

Implementación: `Infrastructure/Reports/QuestPdfReportGenerator.cs`, registrada como singleton en
`DependencyInjection.AddInfrastructure`.

---

## Infraestructura

1. `QuestPDF` en **Infrastructure.csproj** (versión estable actual de la serie 2026).
2. `Program.cs`, antes de construir la app:
   ```csharp
   QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
   QuestPDF.Settings.UseEnvironmentFonts = false; // el contenedor no trae fuentes del sistema
   ```
   Con `UseEnvironmentFonts = false` se usa la fuente que QuestPDF trae embebida, y el PDF sale
   igual en Windows y en el contenedor.
3. `api/Dockerfile`, etapa runtime: agregar `libfontconfig1` al `apt-get install` que ya instala
   `curl`. Sin eso, SkiaSharp puede fallar al cargar en la imagen Debian slim.

---

## Frontend

### Pantalla nueva: Configuración → Marca

`web/src/portal-admin/pages/MarcaPage.tsx`, ruta `configuracion/marca`.

Formulario con los campos de la parte A, subida de logo con vista previa, selector de color de
marca. Al guardar, `PUT /organization`. Estados de carga y error como el resto del panel.

### Botón de recibo en `PagosPage.tsx`

Una acción por fila, **sólo en las filas de tipo pago**. Descarga el blob con el patrón ya usado
para el CSV de transacciones. El nombre del archivo sale del header `Content-Disposition`; si no
viene, `recibo.pdf`.

### Pantalla nueva: Rendiciones

`web/src/portal-admin/pages/RendicionesPage.tsx`, ruta `rendiciones`.

Selector de propietario, período desde/hasta (mes y año), botón "Ver". Muestra la tabla de líneas
con totales, y un botón "Descargar PDF". Estado vacío cuando no hay cobranzas en el período.

### Servicios

- `web/src/features/organization/services/organizationService.ts`: `get`, `update`, `uploadLogo`,
  `deleteLogo`, `logoUrl`.
- En el servicio de propietarios: `getSettlement(ownerId, from, to)` y `downloadSettlementPdf(...)`.
- En `contractService.ts`: `downloadReceiptPdf(transactionId)`.

TypeScript strict: sin `any`, sin `@ts-ignore`. `pnpm lint` tiene que quedar en 0 warnings.

---

## Criterios de terminado

1. `dotnet build` y `pnpm build` limpios. `pnpm lint` en 0/0.
2. Tests nuevos en verde:
   - `AmountInWords` con todos los casos listados arriba.
   - Recibo: transacción que no es pago → 409. Transacción inexistente → 404.
   - Numeración: pedir dos veces el recibo de la misma transacción devuelve el **mismo** número;
     dos transacciones distintas devuelven números **consecutivos**.
   - Validación de la marca: color inválido, email inválido, logo de tipo no permitido, logo > 2 MB.
   - Aislamiento multi-tenant: un admin de la organización A no obtiene el recibo de una
     transacción de la organización B (404, no 200).
3. Los dos PDF abren en un lector real y muestran la marca cargada.
4. El test preexistente `StorageProviderValidationTests` sigue rojo por una causa no relacionada;
   no cuenta como regresión.
