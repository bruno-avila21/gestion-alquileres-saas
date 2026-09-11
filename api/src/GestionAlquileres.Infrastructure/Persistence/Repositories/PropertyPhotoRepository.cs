using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence.Repositories;

public class PropertyPhotoRepository : IPropertyPhotoRepository
{
    private readonly AppDbContext _db;
    public PropertyPhotoRepository(AppDbContext db) => _db = db;

    public Task<PropertyPhoto?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.PropertyPhotos.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<PropertyPhoto>> GetByPropertyAsync(Guid propertyId, CancellationToken ct) =>
        await _db.PropertyPhotos.Where(p => p.PropertyId == propertyId)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<PropertyPhoto>> GetByPropertiesAsync(
        IReadOnlyCollection<Guid> propertyIds, CancellationToken ct) =>
        await _db.PropertyPhotos.AsNoTracking()
            .Where(p => propertyIds.Contains(p.PropertyId))
            .OrderBy(p => p.SortOrder).ThenBy(p => p.CreatedAt).ToListAsync(ct);

    public Task<int> CountByPropertyAsync(Guid propertyId, CancellationToken ct) =>
        _db.PropertyPhotos.CountAsync(p => p.PropertyId == propertyId, ct);

    public async Task AddAsync(PropertyPhoto photo, CancellationToken ct) =>
        await _db.PropertyPhotos.AddAsync(photo, ct);

    public void Remove(PropertyPhoto photo) => _db.PropertyPhotos.Remove(photo);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
