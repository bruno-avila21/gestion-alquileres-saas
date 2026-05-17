using GestionAlquileres.Application.Features.AppTenants.Commands;
using GestionAlquileres.Application.Features.AppTenants.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.AppTenants.Queries;

public class GetAppTenantsQueryHandler : IRequestHandler<GetAppTenantsQuery, IReadOnlyList<AppTenantDto>>
{
    private readonly IAppTenantRepository _repo;

    public GetAppTenantsQueryHandler(IAppTenantRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<AppTenantDto>> Handle(GetAppTenantsQuery request, CancellationToken ct)
    {
        var tenants = await _repo.GetAllAsync(ct);
        return tenants.Select(CreateAppTenantCommandHandler.ToDto).ToList();
    }
}
