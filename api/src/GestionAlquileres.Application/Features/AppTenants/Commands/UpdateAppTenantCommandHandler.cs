using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.AppTenants.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.AppTenants.Commands;

public class UpdateAppTenantCommandHandler : IRequestHandler<UpdateAppTenantCommand, AppTenantDto>
{
    private readonly IAppTenantRepository _repo;

    public UpdateAppTenantCommandHandler(IAppTenantRepository repo) => _repo = repo;

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
        tenant.IsActive = request.IsActive;

        await _repo.SaveChangesAsync(ct);

        return CreateAppTenantCommandHandler.ToDto(tenant);
    }
}
