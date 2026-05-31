using GestionAlquileres.Application.Features.RentHistory.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.RentHistory.Commands;

public record ApplyRentAdjustmentCommand(
    Guid ContractId,
    DateOnly? EffectiveDate = null,
    decimal? ManualNewRent = null,
    string? Notes = null
) : IRequest<RentHistoryDto>;
