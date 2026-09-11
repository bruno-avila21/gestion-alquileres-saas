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
    DateTimeOffset CreatedAt,
    // Ficha pública
    string? Neighborhood,
    string? Code,
    string? Description,
    int? Rooms,
    int? Bedrooms,
    int? Bathrooms,
    int? Garages,
    int? AgeYears,
    decimal? CoveredAreaM2,
    double? Latitude,
    double? Longitude,
    bool? SuitableForCredit,
    IReadOnlyList<string> Features);

/// <summary>Campos de la ficha pública compartidos por los comandos de alta y edición.</summary>
public record PropertyListingDetails(
    string? Neighborhood,
    string? Code,
    string? Description,
    int? Rooms,
    int? Bedrooms,
    int? Bathrooms,
    int? Garages,
    int? AgeYears,
    decimal? CoveredAreaM2,
    double? Latitude,
    double? Longitude,
    bool? SuitableForCredit,
    IReadOnlyList<string>? Features)
{
    public static readonly PropertyListingDetails Empty =
        new(null, null, null, null, null, null, null, null, null, null, null, null, null);
}
