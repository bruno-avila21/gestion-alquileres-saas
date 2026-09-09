using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Organizations.Commands;

public class DeleteOrganizationLogoCommandHandler : IRequestHandler<DeleteOrganizationLogoCommand>
{
    private readonly IOrganizationRepository _repo;
    private readonly IStorageService _storage;
    private readonly ICurrentTenant _currentTenant;

    public DeleteOrganizationLogoCommandHandler(
        IOrganizationRepository repo, IStorageService storage, ICurrentTenant currentTenant)
    {
        _repo = repo;
        _storage = storage;
        _currentTenant = currentTenant;
    }

    public async Task Handle(DeleteOrganizationLogoCommand request, CancellationToken ct)
    {
        var org = await _repo.GetByIdAsync(_currentTenant.OrganizationId, ct)
            ?? throw new InvalidOperationException("La organización del token no existe.");

        if (string.IsNullOrWhiteSpace(org.LogoStorageKey))
            return; // nada que borrar — idempotente.

        var key = org.LogoStorageKey;
        org.LogoStorageKey = null;
        await _repo.SaveChangesAsync(ct);
        await _storage.DeleteAsync(key, ct);
    }
}
