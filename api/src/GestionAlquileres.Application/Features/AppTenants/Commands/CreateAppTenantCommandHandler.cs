using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.AppTenants.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.AppTenants.Commands;

public class CreateAppTenantCommandHandler : IRequestHandler<CreateAppTenantCommand, AppTenantDto>
{
    private readonly IAppTenantRepository _repo;

    public CreateAppTenantCommandHandler(IAppTenantRepository repo) => _repo = repo;

    public async Task<AppTenantDto> Handle(CreateAppTenantCommand request, CancellationToken ct)
    {
        if (await _repo.DniExistsAsync(request.Dni, ct))
            throw new BusinessException($"Ya existe un inquilino con DNI {request.Dni} en esta organización.");

        var tenant = new AppTenant
        {
            OrganizationId = request.OrganizationId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Dni = request.Dni.Trim(),
            Email = request.Email?.Trim().ToLowerInvariant(),
            Phone = request.Phone?.Trim(),
        };

        await _repo.AddAsync(tenant, ct);
        await _repo.SaveChangesAsync(ct);

        return ToDto(tenant);
    }

    internal static AppTenantDto ToDto(AppTenant t) =>
        new(t.Id, t.OrganizationId, t.FirstName, t.LastName, t.Dni,
            t.Email, t.Phone, t.UserId, t.IsActive, t.CreatedAt);
}
