using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.API.Contracts;

public record UpdateContractRequest(
    Guid PropertyId,
    Guid AppTenantId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal MonthlyRent,
    Currency Currency,
    AdjustmentType AdjustmentType,
    AdjustmentFrequency AdjustmentFrequency,
    /// <summary>Requerido sólo cuando AdjustmentType es FixedPercent (ej. 8 para un 8%).</summary>
    decimal? AdjustmentPercent,
    int DayOfMonth,
    decimal? DepositAmount,
    string? Notes
);
