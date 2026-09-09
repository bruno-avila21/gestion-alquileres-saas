using GestionAlquileres.Application.Features.Public.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Public.Queries;

public record GetPublicOrganizationQuery(string Slug) : IRequest<PublicOrganizationDto?>;

public class GetPublicOrganizationQueryHandler : IRequestHandler<GetPublicOrganizationQuery, PublicOrganizationDto?>
{
    private readonly IOrganizationRepository _orgs;
    public GetPublicOrganizationQueryHandler(IOrganizationRepository orgs) => _orgs = orgs;

    public async Task<PublicOrganizationDto?> Handle(GetPublicOrganizationQuery request, CancellationToken ct)
    {
        var org = await _orgs.GetBySlugAsync(request.Slug.ToLowerInvariant(), ct);
        // A suspended organization's site goes dark along with its panel.
        return org is null || !org.IsActive ? null : new PublicOrganizationDto(org.Name, org.Slug);
    }
}
