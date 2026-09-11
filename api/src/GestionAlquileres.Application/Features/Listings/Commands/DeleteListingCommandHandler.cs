using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Listings.Commands;

public class DeleteListingCommandHandler : IRequestHandler<DeleteListingCommand>
{
    private readonly IListingRepository _listings;

    public DeleteListingCommandHandler(IListingRepository listings) => _listings = listings;

    public async Task Handle(DeleteListingCommand request, CancellationToken ct)
    {
        var listing = await _listings.GetByIdAsync(request.Id, ct)
            ?? throw new BusinessException($"Listing {request.Id} not found.");

        // Hard delete: a listing carries no money movements (those hang off Contract), so there is
        // nothing to preserve. Sold/rented history is kept by setting the status instead.
        _listings.Remove(listing);
        await _listings.SaveChangesAsync(ct);
    }
}
