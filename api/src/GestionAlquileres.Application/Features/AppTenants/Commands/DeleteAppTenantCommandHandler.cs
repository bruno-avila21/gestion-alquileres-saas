using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.AppTenants.Commands;

public class DeleteAppTenantCommandHandler : IRequestHandler<DeleteAppTenantCommand>
{
    private readonly IAppTenantRepository _repo;
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;

    public DeleteAppTenantCommandHandler(
        IAppTenantRepository repo, IUserRepository users, IRefreshTokenRepository refreshTokens)
    {
        _repo = repo;
        _users = users;
        _refreshTokens = refreshTokens;
    }

    public async Task Handle(DeleteAppTenantCommand request, CancellationToken ct)
    {
        var tenant = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new BusinessException($"Inquilino {request.Id} not found.");

        tenant.IsActive = false;

        // La baja tiene que cortar el acceso al portal, no sólo ocultar al inquilino de los listados.
        await TenantAccessRevoker.RevokeAsync(tenant, _users, _refreshTokens, ct);

        await _repo.SaveChangesAsync(ct);
    }
}
