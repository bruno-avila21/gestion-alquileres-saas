using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Listings.DTOs;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Listings.Commands;

public class UpdateListingCommandHandler : IRequestHandler<UpdateListingCommand, ListingDto>
{
    private readonly IListingRepository _listings;

    public UpdateListingCommandHandler(IListingRepository listings) => _listings = listings;

    public async Task<ListingDto> Handle(UpdateListingCommand request, CancellationToken ct)
    {
        var listing = await _listings.GetByIdAsync(request.Id, ct)
            ?? throw new BusinessException($"Listing {request.Id} not found.");

        if (request.Status == ListingStatus.Published)
        {
            var siblings = await _listings.GetAllAsync(listing.PropertyId, ct);
            if (siblings.Any(l => l.Id != listing.Id && l.OperationType == request.OperationType && l.Status == ListingStatus.Published))
                throw new BusinessException("La propiedad ya tiene una publicación activa para esa operación.");
        }

        var becomesPublished = listing.Status != ListingStatus.Published && request.Status == ListingStatus.Published;

        listing.OperationType = request.OperationType;
        listing.Price = request.Price;
        listing.Currency = request.Currency;
        listing.Expenses = request.Expenses;
        listing.Title = request.Title.Trim();
        listing.IsFeatured = request.IsFeatured;
        listing.Status = request.Status;
        listing.UpdatedAt = DateTimeOffset.UtcNow;
        if (becomesPublished) listing.PublishedAt = listing.UpdatedAt;

        await _listings.SaveChangesAsync(ct);
        return ListingDto.From(listing);
    }
}
