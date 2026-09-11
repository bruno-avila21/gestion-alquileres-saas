using GestionAlquileres.Application.Features.SiteSettings.DTOs;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Application.Features.Public.DTOs;

/// <summary>
/// Lo que el sitio público sabe de la inmobiliaria. Sin datos internos (plan, estado, CUIT):
/// sólo lo que la inmobiliaria ya publica de puertas afuera para que la contacten.
/// </summary>
public record PublicOrganizationDto(
    string Name,
    string Slug,
    string? Address,
    string? Phone,
    string? Email,
    /// <summary>
    /// Si la inmobiliaria cargó su logo en Configuración → Marca. El archivo se pide por
    /// <c>GET /public/{slug}/logo</c>; acá sólo viaja el booleano para que el sitio sepa si
    /// dibujar la imagen o el monograma con la inicial.
    /// </summary>
    bool HasLogo,
    /// <summary>Aspecto y textos elegidos por la inmobiliaria. Campos en null = usar el diseño por defecto.</summary>
    SiteSettingsDto Site);

/// <summary>Tarjeta del listado público.</summary>
public record PublicListingCardDto(
    Guid Id,
    OperationType OperationType,
    decimal Price,
    Currency Currency,
    decimal? Expenses,
    string Title,
    bool IsFeatured,
    PropertyType PropertyType,
    string Address,
    string? Neighborhood,
    string City,
    string? Code,
    int? Rooms,
    int? Bedrooms,
    int? Bathrooms,
    int? Garages,
    decimal? AreaM2,
    decimal? CoveredAreaM2,
    string? CoverPhotoUrl,
    DateTimeOffset? PublishedAt);

/// <summary>Ficha completa de una publicación.</summary>
public record PublicListingDetailDto(
    Guid Id,
    OperationType OperationType,
    decimal Price,
    Currency Currency,
    decimal? Expenses,
    string Title,
    bool IsFeatured,
    PropertyType PropertyType,
    string Address,
    string? Neighborhood,
    string City,
    string Province,
    string? Code,
    string? Description,
    int? Rooms,
    int? Bedrooms,
    int? Bathrooms,
    int? Garages,
    int? AgeYears,
    decimal? AreaM2,
    decimal? CoveredAreaM2,
    double? Latitude,
    double? Longitude,
    bool? SuitableForCredit,
    IReadOnlyList<string> Features,
    IReadOnlyList<string> PhotoUrls,
    DateTimeOffset? PublishedAt);

public record FacetDto(string Value, int Count);

/// <summary>Contadores por faceta sobre el conjunto filtrado, como los muestra el buscador (“Departamento (3)”).</summary>
public record PublicListingFacetsDto(
    IReadOnlyList<FacetDto> OperationTypes,
    IReadOnlyList<FacetDto> PropertyTypes,
    IReadOnlyList<FacetDto> Cities,
    IReadOnlyList<FacetDto> Neighborhoods,
    IReadOnlyList<FacetDto> Currencies,
    IReadOnlyList<FacetDto> Rooms,
    IReadOnlyList<FacetDto> Bedrooms,
    IReadOnlyList<FacetDto> Features,
    IReadOnlyList<FacetDto> SuitableForCredit);

public record PublicListingSearchResultDto(
    IReadOnlyList<PublicListingCardDto> Items,
    int Total,
    int Page,
    int PageSize,
    PublicListingFacetsDto Facets);
