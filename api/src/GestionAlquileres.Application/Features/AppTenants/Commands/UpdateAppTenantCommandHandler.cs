using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.AppTenants.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.AppTenants.Commands;

public class UpdateAppTenantCommandHandler : IRequestHandler<UpdateAppTenantCommand, AppTenantDto>
{
    private readonly IAppTenantRepository _repo;
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;

    public UpdateAppTenantCommandHandler(
        IAppTenantRepository repo, IUserRepository users, IRefreshTokenRepository refreshTokens)
    {
        _repo = repo;
        _users = users;
        _refreshTokens = refreshTokens;
    }

    public async Task<AppTenantDto> Handle(UpdateAppTenantCommand request, CancellationToken ct)
    {
        var tenant = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new BusinessException($"Inquilino {request.Id} not found.");

        if (tenant.Dni != request.Dni.Trim() && await _repo.DniExistsAsync(request.Dni.Trim(), ct))
            throw new BusinessException($"Ya existe un inquilino con DNI {request.Dni} en esta organización.");

        tenant.FirstName = request.FirstName.Trim();
        tenant.LastName = request.LastName.Trim();
        tenant.Dni = request.Dni.Trim();
        tenant.Email = request.Email?.Trim().ToLowerInvariant();
        tenant.Phone = request.Phone?.Trim();
        var wasActive = tenant.IsActive;
        tenant.IsActive = request.IsActive;

        // Desactivar desde la edición tiene que cortar el acceso igual que la baja.
        if (wasActive && !request.IsActive)
            await TenantAccessRevoker.RevokeAsync(tenant, _users, _refreshTokens, ct);

        await _repo.SaveChangesAsync(ct);

        return CreateAppTenantCommandHandler.ToDto(tenant);
    }
}
