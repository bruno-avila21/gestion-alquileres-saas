using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence.Repositories;

public class OwnerRepository : IOwnerRepository
{
    private readonly AppDbContext _db;
    public OwnerRepository(AppDbContext db) => _db = db;

    public Task<Owner?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Owners.FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Owner>> GetAllAsync(CancellationToken ct) =>
        await _db.Owners.OrderBy(o => o.Name).ToListAsync(ct);

    public async Task AddAsync(Owner owner, CancellationToken ct) =>
        await _db.Owners.AddAsync(owner, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct) =>
        _db.Owners.AnyAsync(o => o.Id == id, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
