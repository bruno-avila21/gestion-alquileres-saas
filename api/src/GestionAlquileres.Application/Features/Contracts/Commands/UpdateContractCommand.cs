using GestionAlquileres.Application.Features.Contracts.DTOs;
using GestionAlquileres.Domain.Enums;
using MediatR;

namespace GestionAlquileres.Application.Features.Contracts.Commands;

public record UpdateContractCommand(
    Guid Id,
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
) : IRequest<ContractDto?>;
