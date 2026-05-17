using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface IAppTenantRepository
{
    Task<AppTenant?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<AppTenant>> GetAllAsync(CancellationToken ct);
    Task AddAsync(AppTenant tenant, CancellationToken ct);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct);
    Task<bool> DniExistsAsync(string dni, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
