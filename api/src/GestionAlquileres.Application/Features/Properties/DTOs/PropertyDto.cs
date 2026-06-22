using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Application.Features.Properties.DTOs;

public record PropertyDto(
    Guid Id,
    Guid OrganizationId,
    string Address,
    string City,
    string Province,
    PropertyType PropertyType,
    decimal? AreaM2,
    string? Notes,
    Guid? OwnerId,
    decimal? CommissionPct,
    bool IsActive,
    DateTimeOffset CreatedAt);
