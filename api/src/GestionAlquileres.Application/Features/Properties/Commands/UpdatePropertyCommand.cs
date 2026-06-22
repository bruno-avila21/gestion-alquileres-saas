using GestionAlquileres.Application.Features.Properties.DTOs;
using GestionAlquileres.Domain.Enums;
using MediatR;

namespace GestionAlquileres.Application.Features.Properties.Commands;

public record UpdatePropertyCommand(
    Guid Id,
    string Address,
    string City,
    string Province,
    PropertyType PropertyType,
    decimal? AreaM2,
    string? Notes,
    Guid? OwnerId,
    decimal? CommissionPct,
    bool IsActive)
    : IRequest<PropertyDto>;
