using GestionAlquileres.Application.Features.Listings.DTOs;
using GestionAlquileres.Domain.Enums;
using MediatR;

namespace GestionAlquileres.Application.Features.Listings.Commands;

public record CreateListingCommand(
    Guid PropertyId,
    OperationType OperationType,
    decimal Price,
    Currency Currency,
    decimal? Expenses,
    string Title,
    bool IsFeatured,
    ListingStatus Status)
    : IRequest<ListingDto>;
