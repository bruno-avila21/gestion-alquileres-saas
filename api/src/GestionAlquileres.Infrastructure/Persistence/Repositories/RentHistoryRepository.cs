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
}
