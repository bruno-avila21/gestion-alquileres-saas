namespace GestionAlquileres.Domain.Reports;

/// <summary>
/// Marca de la inmobiliaria que aparece en el encabezado de cada PDF. Es una foto de los datos de
/// <c>Organization</c> en el momento de generar el documento — no se persiste aparte.
/// </summary>
public record AgencyBrand(
    string Name,
    string? LegalName,
    string? TaxId,
    string? Address,
    string? Phone,
    string? Email,
    byte[]? Logo,
    string? BrandColor);

/// <summary>
/// Recibo de pago. El PDF se genera en cada descarga desde datos vivos — este record es la
/// instantánea de lo que hay que imprimir, ya resuelta (sin ids, sin navegación a otras entidades).
/// </summary>
public record ReceiptReport(
    string Number,
    DateOnly IssuedOn,
    AgencyBrand Agency,
    string PayerName,
    string? PayerDocument,
    string PropertyAddress,
    string Concept,
    decimal Amount,
    string CurrencyCode,
    string AmountInWords,
    string? Notes);

/// <summary>Una línea de la liquidación: lo cobrado por una propiedad del propietario en el período.</summary>
public record OwnerSettlementReportLine(
    string PropertyAddress,
    decimal Collected,
    decimal CommissionPct,
    decimal Commission,
    decimal Net);

/// <summary>Liquidación al propietario para un rango de períodos. Reusa el cálculo ya hecho por GetOwnerSettlementQuery.</summary>
public record OwnerSettlementReport(
    AgencyBrand Agency,
    string OwnerName,
    string? OwnerTaxId,
    string? OwnerCbu,
    DateOnly PeriodFrom,
    DateOnly PeriodTo,
    decimal GrossCollected,
    decimal Commission,
    decimal NetToOwner,
    IReadOnlyList<OwnerSettlementReportLine> Lines);
