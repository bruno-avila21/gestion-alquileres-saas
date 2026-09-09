using GestionAlquileres.Application.Common;
using GestionAlquileres.Application.Features.Public.DTOs;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Public.Queries;

public record GetPublicListingQuery(string Slug, Guid Id) : IRequest<PublicListingDetailDto?>;

public class GetPublicListingQueryHandler : IRequestHandler<GetPublicListingQuery, PublicListingDetailDto?>
{
    private readonly IOrganizationRepository _orgs;
    private readonly IListingRepository _listings;
    private readonly IPropertyPhotoRepository _photos;

    public GetPublicListingQueryHandler(IOrganizationRepository orgs, IListingRepository listings, IPropertyPhotoRepository photos)
    {
        _orgs = orgs;
        _listings = listings;
        _photos = photos;
    }

    public async Task<PublicListingDetailDto?> Handle(GetPublicListingQuery request, CancellationToken ct)
    {
        var org = await _orgs.GetBySlugAsync(request.Slug.ToLowerInvariant(), ct);
        if (org is null || !org.IsActive) return null;

        var l = await _listings.GetByIdAsync(request.Id, ct);
        // Drafts, paused and sold listings are not a 403 but a 404: the public never learns they exist.
        if (l is null || l.Status != ListingStatus.Published || !l.Property.IsActive) return null;

        var photos = await _photos.GetByPropertyAsync(l.PropertyId, ct);
        var urls = photos.OrderByDescending(p => p.IsCover).ThenBy(p => p.SortOrder)
            .Select(p => PublicUrls.Photo(org.Slug, p.Id)).ToList();

        var p = l.Property;
        return new PublicListingDetailDto(
            l.Id, l.OperationType, l.Price, l.Currency, l.Expenses, l.Title, l.IsFeatured,
            p.PropertyType, p.Address, p.Neighborhood, p.City, p.Province, p.Code, p.Description,
            p.Rooms, p.Bedrooms, p.Bathrooms, p.Garages, p.AgeYears, p.AreaM2, p.CoveredAreaM2,
            p.Latitude, p.Longitude, p.SuitableForCredit, p.Features.AsReadOnly(), urls, l.PublishedAt);
    }
}
