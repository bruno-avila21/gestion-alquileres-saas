using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.API.Contracts;

public record CreateListingRequest(
    Guid PropertyId,
    OperationType OperationType,
    decimal Price,
    Currency Currency,
    decimal? Expenses,
    string Title,
    bool IsFeatured = false,
    ListingStatus Status = ListingStatus.Draft);

public record UpdateListingRequest(
    OperationType OperationType,
    decimal Price,
    Currency Currency,
    decimal? Expenses,
    string Title,
    bool IsFeatured,
    ListingStatus Status);
