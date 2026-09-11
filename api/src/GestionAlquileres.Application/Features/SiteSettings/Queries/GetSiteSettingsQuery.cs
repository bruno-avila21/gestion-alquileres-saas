using GestionAlquileres.Application.Features.SiteSettings.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.SiteSettings.Queries;

public record GetSiteSettingsQuery : IRequest<SiteSettingsDto>;

public class GetSiteSettingsQueryHandler : IRequestHandler<GetSiteSettingsQuery, SiteSettingsDto>
{
    private readonly ISiteSettingsRepository _repo;
    public GetSiteSettingsQueryHandler(ISiteSettingsRepository repo) => _repo = repo;

    public async Task<SiteSettingsDto> Handle(GetSiteSettingsQuery request, CancellationToken ct) =>
        SiteSettingsDto.From(await _repo.GetAsync(ct));
}
