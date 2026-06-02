using GestionAlquileres.Application.Features.RentHistory.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.RentHistory.Commands;

public record ApplyRentAdjustmentCommand(
    Guid ContractId,
    DateOnly? EffectiveDate = null,
    decimal? ManualNewRent = null,
    string? Notes = null,
    // Set ONLY by background jobs (no tenant in scope) so the raw lookup stays pinned to one org.
    // HTTP handlers leave this null and resolve the contract through the tenant-filtered path.
    Guid? OrganizationId = null
) : IRequest<RentHistoryDto>;
