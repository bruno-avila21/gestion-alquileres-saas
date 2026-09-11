using GestionAlquileres.Application.Features.Listings.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Listings.Queries;

public record GetListingsQuery(Guid? PropertyId) : IRequest<IReadOnlyList<ListingDto>>;

public class GetListingsQueryHandler : IRequestHandler<GetListingsQuery, IReadOnlyList<ListingDto>>
{
    private readonly IListingRepository _listings;
    public GetListingsQueryHandler(IListingRepository listings) => _listings = listings;

    public async Task<IReadOnlyList<ListingDto>> Handle(GetListingsQuery request, CancellationToken ct) =>
        (await _listings.GetAllAsync(request.PropertyId, ct)).Select(ListingDto.From).ToList();
}

public record GetListingByIdQuery(Guid Id) : IRequest<ListingDto?>;

public class GetListingByIdQueryHandler : IRequestHandler<GetListingByIdQuery, ListingDto?>
{
    private readonly IListingRepository _listings;
    public GetListingByIdQueryHandler(IListingRepository listings) => _listings = listings;

    public async Task<ListingDto?> Handle(GetListingByIdQuery request, CancellationToken ct)
    {
        var listing = await _listings.GetByIdAsync(request.Id, ct);
        return listing is null ? null : ListingDto.From(listing);
    }
}
