using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface IPropertyRepository
{
    Task<Property?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Property>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<Property>> GetByOwnerAsync(Guid ownerId, CancellationToken ct);
    Task AddAsync(Property property, CancellationToken ct);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
