using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Application.Features.Listings.DTOs;

public record ListingDto(
    Guid Id,
    Guid PropertyId,
    OperationType OperationType,
    decimal Price,
    Currency Currency,
    decimal? Expenses,
    ListingStatus Status,
    string Title,
    bool IsFeatured,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    // Resumen de la propiedad para el listado del panel
    string PropertyAddress,
    string PropertyCity,
    string? PropertyNeighborhood,
    PropertyType PropertyType,
    string? PropertyCode)
{
    public static ListingDto From(Listing l) =>
        new(l.Id, l.PropertyId, l.OperationType, l.Price, l.Currency, l.Expenses, l.Status, l.Title,
            l.IsFeatured, l.PublishedAt, l.CreatedAt, l.UpdatedAt,
            l.Property.Address, l.Property.City, l.Property.Neighborhood, l.Property.PropertyType, l.Property.Code);
}
