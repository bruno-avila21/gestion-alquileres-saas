using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence.Repositories;

public class RentHistoryRepository : IRentHistoryRepository
{
    private readonly AppDbContext _db;
    public RentHistoryRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<RentHistory>> GetByContractAsync(Guid contractId, CancellationToken ct) =>
        await _db.RentHistory
            .Where(r => r.ContractId == contractId)
            .OrderByDescending(r => r.EffectiveDate)
            .ToListAsync(ct);

    public Task<RentHistory?> GetLastByContractAsync(Guid contractId, CancellationToken ct) =>
        _db.RentHistory
            .Where(r => r.ContractId == contractId)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<RentHistory>> GetAllAsync(CancellationToken ct) =>
        await _db.RentHistory
            .OrderByDescending(r => r.EffectiveDate)
            .ThenByDescending(r => r.CreatedAt)
            .Take(500)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<RentHistory> Items, int Total)> GetPagedAsync(
        Domain.Enums.AdjustmentType? type, string? search, int page, int pageSize, CancellationToken ct)
    {
        // All DbSets carry the global tenant filter, so the join stays scoped to the current org.
        var query =
            from r in _db.RentHistory
            join c in _db.Contracts on r.ContractId equals c.Id
            join t in _db.AppTenants on c.AppTenantId equals t.Id
            join p in _db.Properties on c.PropertyId equals p.Id
            select new { r, t, p };

        if (type.HasValue)
            query = query.Where(x => x.r.AdjustmentType == type.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.t.FirstName + " " + x.t.LastName).ToLower().Contains(s) ||
                x.p.Address.ToLower().Contains(s) ||
                (x.r.Notes ?? "").ToLower().Contains(s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.r.EffectiveDate)
            .ThenByDescending(x => x.r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.r)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(RentHistory record, CancellationToken ct) =>
        await _db.RentHistory.AddAsync(record, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    public Task<bool> ExistsForPeriodAsync(Guid contractId, DateOnly effectiveDate, CancellationToken ct) =>
        _db.RentHistory
            .IgnoreQueryFilters()
            .AnyAsync(r => r.ContractId == contractId && r.EffectiveDate == effectiveDate, ct);

    public Task<RentHistory?> GetLastByContractRawAsync(Guid contractId, CancellationToken ct) =>
        _db.RentHistory
            .IgnoreQueryFilters()
            .Where(r => r.ContractId == contractId)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync(ct);

    public Task<int> CountByContractRawAsync(Guid contractId, CancellationToken ct) =>
        _db.RentHistory
            .IgnoreQueryFilters()
            .CountAsync(r => r.ContractId == contractId, ct);
}
