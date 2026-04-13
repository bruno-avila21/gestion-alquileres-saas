using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence.Repositories;

public class IndexRepository : IIndexRepository
{
    private readonly AppDbContext _db;
    public IndexRepository(AppDbContext db) => _db = db;

    // No IgnoreQueryFilters() needed — IndexValue is global (no filter registered).
    public Task<IndexValue?> GetByPeriodAsync(IndexType type, DateOnly period, CancellationToken ct) =>
        _db.Indexes.FirstOrDefaultAsync(x => x.IndexType == type && x.Period == period, ct);

    public Task<IndexValue?> GetLastAvailableAsync(IndexType type, CancellationToken ct) =>
        _db.Indexes.Where(x => x.IndexType == type)
                   .OrderByDescending(x => x.Period)
                   .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<IndexValue>> GetRangeAsync(
        IndexType type, DateOnly from, DateOnly to, CancellationToken ct)
    {
        return await _db.Indexes
            .Where(x => x.IndexType == type && x.Period >= from && x.Period <= to)
            .OrderBy(x => x.Period)
            .ToListAsync(ct);
    }

    public async Task AddAsync(IndexValue indexValue, CancellationToken ct) =>
        await _db.Indexes.AddAsync(indexValue, ct);

    public Task<bool> ExistsAsync(IndexType type, DateOnly period, CancellationToken ct) =>
        _db.Indexes.AnyAsync(x => x.IndexType == type && x.Period == period, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
