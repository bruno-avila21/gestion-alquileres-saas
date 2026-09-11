using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface IPropertyPhotoRepository
{
    Task<PropertyPhoto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<PropertyPhoto>> GetByPropertyAsync(Guid propertyId, CancellationToken ct);

    /// <summary>Photos of many properties at once (listado público / admin), ordered by SortOrder.</summary>
    Task<IReadOnlyList<PropertyPhoto>> GetByPropertiesAsync(IReadOnlyCollection<Guid> propertyIds, CancellationToken ct);
    Task<int> CountByPropertyAsync(Guid propertyId, CancellationToken ct);
    Task AddAsync(PropertyPhoto photo, CancellationToken ct);
    void Remove(PropertyPhoto photo);
    Task SaveChangesAsync(CancellationToken ct);
}
