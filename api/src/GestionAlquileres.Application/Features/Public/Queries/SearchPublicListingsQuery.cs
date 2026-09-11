using FluentValidation;
using GestionAlquileres.Application.Common;
using GestionAlquileres.Application.Features.Public.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Public.Queries;

/// <summary>
/// Buscador del sitio público. Todos los filtros son opcionales; <see cref="Sort"/> admite
/// price_asc, price_desc, rooms_asc, rooms_desc, newest (default: destacadas primero, luego más nuevas).
/// </summary>
public record SearchPublicListingsQuery(
    string Slug,
    OperationType? OperationType,
    PropertyType? PropertyType,
    string? City,
    string? Neighborhood,
    Currency? Currency,
    decimal? MinPrice,
    decimal? MaxPrice,
    int? MinRooms,
    int? MinBedrooms,
    decimal? MinAreaM2,
    decimal? MaxAreaM2,
    IReadOnlyList<string>? Features,
    bool? SuitableForCredit,
    string? Sort,
    int Page = 1,
    int PageSize = 24)
    : IRequest<PublicListingSearchResultDto?>;

public class SearchPublicListingsQueryValidator : AbstractValidator<SearchPublicListingsQuery>
{
    public SearchPublicListingsQueryValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Neighborhood).MaximumLength(100);
        RuleFor(x => x.Features!).Must(f => f.Count <= 20).When(x => x.Features is not null);
    }
}

public class SearchPublicListingsQueryHandler : IRequestHandler<SearchPublicListingsQuery, PublicListingSearchResultDto?>
{
    private readonly IOrganizationRepository _orgs;
    private readonly IListingRepository _listings;
    private readonly IPropertyPhotoRepository _photos;

    public SearchPublicListingsQueryHandler(
        IOrganizationRepository orgs, IListingRepository listings, IPropertyPhotoRepository photos)
    {
        _orgs = orgs;
        _listings = listings;
        _photos = photos;
    }

    public async Task<PublicListingSearchResultDto?> Handle(SearchPublicListingsQuery q, CancellationToken ct)
    {
        var org = await _orgs.GetBySlugAsync(q.Slug.ToLowerInvariant(), ct);
        if (org is null || !org.IsActive) return null;

        // Tenant-scoped by the global filter (TenantMiddleware resolved the slug). A public site holds
        // tens to a few hundred published listings, so filtering and facets happen in memory; at
        // thousands this moves to the database.
        var all = await _listings.GetPublishedAsync(ct);
        var filtered = Apply(all, q).ToList();

        var facets = BuildFacets(filtered);
        var total = filtered.Count;
        var page = Sort(filtered, q.Sort).Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToList();

        var covers = (await _photos.GetByPropertiesAsync(page.Select(l => l.PropertyId).Distinct().ToList(), ct))
            .GroupBy(p => p.PropertyId)
            .ToDictionary(g => g.Key, g => g.FirstOrDefault(p => p.IsCover) ?? g.First());

        var items = page.Select(l => ToCard(l, covers.TryGetValue(l.PropertyId, out var c) ? PublicUrls.Photo(org.Slug, c.Id) : null)).ToList();
        return new PublicListingSearchResultDto(items, total, q.Page, q.PageSize, facets);
    }

    private static IEnumerable<Listing> Apply(IEnumerable<Listing> src, SearchPublicListingsQuery q)
    {
        if (q.OperationType is { } op) src = src.Where(l => l.OperationType == op);
        if (q.PropertyType is { } pt) src = src.Where(l => l.Property.PropertyType == pt);
        if (!string.IsNullOrWhiteSpace(q.City)) src = src.Where(l => Eq(l.Property.City, q.City));
        if (!string.IsNullOrWhiteSpace(q.Neighborhood)) src = src.Where(l => Eq(l.Property.Neighborhood, q.Neighborhood));
        if (q.Currency is { } cur) src = src.Where(l => l.Currency == cur);
        if (q.MinPrice is { } min) src = src.Where(l => l.Price >= min);
        if (q.MaxPrice is { } max) src = src.Where(l => l.Price <= max);
        if (q.MinRooms is { } rooms) src = src.Where(l => l.Property.Rooms >= rooms);
        if (q.MinBedrooms is { } beds) src = src.Where(l => l.Property.Bedrooms >= beds);
        if (q.MinAreaM2 is { } minA) src = src.Where(l => (l.Property.CoveredAreaM2 ?? l.Property.AreaM2) >= minA);
        if (q.MaxAreaM2 is { } maxA) src = src.Where(l => (l.Property.CoveredAreaM2 ?? l.Property.AreaM2) <= maxA);
        if (q.SuitableForCredit is { } credit) src = src.Where(l => l.Property.SuitableForCredit == credit);
        if (q.Features is { Count: > 0 } feats)
            src = src.Where(l => feats.All(f => l.Property.Features.Any(pf => Eq(pf, f))));
        return src;
    }

    private static IEnumerable<Listing> Sort(IEnumerable<Listing> src, string? sort) => sort switch
    {
        "price_asc" => src.OrderBy(l => l.Currency).ThenBy(l => l.Price),
        "price_desc" => src.OrderBy(l => l.Currency).ThenByDescending(l => l.Price),
        "rooms_asc" => src.OrderBy(l => l.Property.Rooms ?? int.MaxValue),
        "rooms_desc" => src.OrderByDescending(l => l.Property.Rooms ?? -1),
        "newest" => src.OrderByDescending(l => l.PublishedAt),
        _ => src.OrderByDescending(l => l.IsFeatured).ThenByDescending(l => l.PublishedAt),
    };

    private static PublicListingFacetsDto BuildFacets(IReadOnlyCollection<Listing> set) => new(
        Count(set, l => l.OperationType.ToString()),
        Count(set, l => l.Property.PropertyType.ToString()),
        Count(set, l => l.Property.City),
        Count(set, l => l.Property.Neighborhood),
        Count(set, l => l.Currency.ToString()),
        CountInt(set, l => l.Property.Rooms),
        CountInt(set, l => l.Property.Bedrooms),
        set.SelectMany(l => l.Property.Features)
            .GroupBy(f => f, StringComparer.OrdinalIgnoreCase)
            .Select(g => new FacetDto(g.Key, g.Count()))
            .OrderByDescending(f => f.Count).ThenBy(f => f.Value).ToList(),
        Count(set, l => l.Property.SuitableForCredit switch { true => "yes", false => "no", null => null }));

    private static List<FacetDto> Count(IEnumerable<Listing> set, Func<Listing, string?> key) =>
        set.Select(key).Where(k => !string.IsNullOrWhiteSpace(k))
            .GroupBy(k => k!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new FacetDto(g.Key, g.Count()))
            .OrderByDescending(f => f.Count).ThenBy(f => f.Value).ToList();

    private static List<FacetDto> CountInt(IEnumerable<Listing> set, Func<Listing, int?> key) =>
        set.Select(key).Where(k => k.HasValue)
            .GroupBy(k => k!.Value)
            .Select(g => new FacetDto(g.Key.ToString(), g.Count()))
            .OrderBy(f => int.Parse(f.Value)).ToList();

    private static bool Eq(string? a, string? b) => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

    internal static PublicListingCardDto ToCard(Listing l, string? coverUrl) =>
        new(l.Id, l.OperationType, l.Price, l.Currency, l.Expenses, l.Title, l.IsFeatured,
            l.Property.PropertyType, l.Property.Address, l.Property.Neighborhood, l.Property.City, l.Property.Code,
            l.Property.Rooms, l.Property.Bedrooms, l.Property.Bathrooms, l.Property.Garages,
            l.Property.AreaM2, l.Property.CoveredAreaM2, coverUrl, l.PublishedAt);
}
