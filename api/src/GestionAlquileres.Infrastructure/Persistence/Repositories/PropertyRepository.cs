using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence.Repositories;

public class PropertyRepository : IPropertyRepository
{
    private readonly AppDbContext _db;
    public PropertyRepository(AppDbContext db) => _db = db;

    public Task<Property?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Properties.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Property>> GetAllAsync(CancellationToken ct) =>
        await _db.Properties.OrderBy(p => p.Address).ToListAsync(ct);

    public async Task AddAsync(Property property, CancellationToken ct) =>
        await _db.Properties.AddAsync(property, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct) =>
        _db.Properties.AnyAsync(p => p.Id == id, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
