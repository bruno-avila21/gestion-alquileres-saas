using GestionAlquileres.Application.Features.Public.DTOs;
using GestionAlquileres.Application.Features.SiteSettings.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Public.Queries;

public record GetPublicOrganizationQuery(string Slug) : IRequest<PublicOrganizationDto?>;

public class GetPublicOrganizationQueryHandler : IRequestHandler<GetPublicOrganizationQuery, PublicOrganizationDto?>
{
    private readonly IOrganizationRepository _orgs;
    private readonly ISiteSettingsRepository _site;

    public GetPublicOrganizationQueryHandler(IOrganizationRepository orgs, ISiteSettingsRepository site)
    {
        _orgs = orgs;
        _site = site;
    }

    public async Task<PublicOrganizationDto?> Handle(GetPublicOrganizationQuery request, CancellationToken ct)
    {
        var org = await _orgs.GetBySlugAsync(request.Slug.ToLowerInvariant(), ct);
        // A suspended organization's site goes dark along with its panel.
        if (org is null || !org.IsActive) return null;

        // El filtro global ya está acotado a esta organización: TenantMiddleware la resolvió
        // desde el slug de la URL antes de llegar al handler.
        var site = await _site.GetAsync(ct);

        return new PublicOrganizationDto(
            org.Name, org.Slug, org.Address, org.Phone, org.Email,
            !string.IsNullOrWhiteSpace(org.LogoStorageKey), SiteSettingsDto.From(site));
    }
}
