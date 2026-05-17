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
    int DayOfMonth,
    decimal? DepositAmount,
    string? Notes
);
