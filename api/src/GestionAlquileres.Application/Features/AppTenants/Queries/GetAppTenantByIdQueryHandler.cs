using GestionAlquileres.Application.Features.AppTenants.Commands;
using GestionAlquileres.Application.Features.AppTenants.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.AppTenants.Queries;

public class GetAppTenantByIdQueryHandler : IRequestHandler<GetAppTenantByIdQuery, AppTenantDto?>
{
    private readonly IAppTenantRepository _repo;

    public GetAppTenantByIdQueryHandler(IAppTenantRepository repo) => _repo = repo;

    public async Task<AppTenantDto?> Handle(GetAppTenantByIdQuery request, CancellationToken ct)
    {
        var tenant = await _repo.GetByIdAsync(request.Id, ct);
        return tenant is null ? null : CreateAppTenantCommandHandler.ToDto(tenant);
    }
}
