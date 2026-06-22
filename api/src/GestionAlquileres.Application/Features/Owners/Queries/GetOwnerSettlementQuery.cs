using GestionAlquileres.Application.Features.Owners.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Owners.Queries;

/// <summary>Computes an owner's settlement for the inclusive month range [PeriodFrom, PeriodTo].</summary>
public record GetOwnerSettlementQuery(
    Guid OwnerId,
    DateOnly PeriodFrom,
    DateOnly PeriodTo) : IRequest<OwnerSettlementDto>;
