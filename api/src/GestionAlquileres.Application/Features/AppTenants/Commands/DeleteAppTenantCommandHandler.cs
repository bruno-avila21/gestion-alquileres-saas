using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.AppTenants.Commands;

public class DeleteAppTenantCommandHandler : IRequestHandler<DeleteAppTenantCommand>
{
    private readonly IAppTenantRepository _repo;

    public DeleteAppTenantCommandHandler(IAppTenantRepository repo) => _repo = repo;

    public async Task Handle(DeleteAppTenantCommand request, CancellationToken ct)
    {
        var tenant = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new BusinessException($"Inquilino {request.Id} not found.");

        tenant.IsActive = false;
        await _repo.SaveChangesAsync(ct);
    }
}
