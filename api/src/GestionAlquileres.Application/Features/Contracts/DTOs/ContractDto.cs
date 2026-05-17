using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Application.Features.Contracts.DTOs;

public record ContractDto(
    Guid Id,
    Guid OrganizationId,
    Guid PropertyId,
    string PropertyAddress,
    string PropertyCity,
    Guid AppTenantId,
    string AppTenantFullName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal MonthlyRent,
    Currency Currency,
    AdjustmentType AdjustmentType,
    AdjustmentFrequency AdjustmentFrequency,
    int DayOfMonth,
    decimal? DepositAmount,
    ContractStatus Status,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? TerminatedAt
);
