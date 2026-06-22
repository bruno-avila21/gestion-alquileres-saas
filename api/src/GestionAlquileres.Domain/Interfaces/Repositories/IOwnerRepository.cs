using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface IOwnerRepository
{
    Task<Owner?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Owner>> GetAllAsync(CancellationToken ct);
    Task AddAsync(Owner owner, CancellationToken ct);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
