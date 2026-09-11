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
    /// <summary>Tasa punitoria diaria en % sobre el capital impago (ej. 0.1 = 0,1% por día). Null o 0 = sin punitorio.</summary>
    decimal? LateFeeDailyRate,
    /// <summary>Días de tolerancia desde el vencimiento antes de que corra el punitorio. 0 = sin tolerancia.</summary>
    int LateFeeGraceDays,
    int DayOfMonth,
    decimal? DepositAmount,
    string? Notes
);
