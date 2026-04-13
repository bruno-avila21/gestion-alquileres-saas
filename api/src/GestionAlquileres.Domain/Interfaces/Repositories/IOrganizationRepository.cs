using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Organization?> GetBySlugAsync(string slug, CancellationToken ct);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct);
    Task AddAsync(Organization org, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
