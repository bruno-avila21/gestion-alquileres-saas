using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _db;
    public TransactionRepository(AppDbContext db) => _db = db;

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Transaction>> GetByContractAsync(Guid contractId, CancellationToken ct) =>
        await _db.Transactions
            .Where(t => t.ContractId == contractId)
            .OrderByDescending(t => t.Period)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Transaction>> GetPendingChargesAsync(Guid contractId, CancellationToken ct) =>
        await _db.Transactions
            .Where(t => t.ContractId == contractId
                        && t.Status == TransactionStatus.Pending
                        && (t.Type == TransactionType.RentCharge || t.Type == TransactionType.ManualDebit))
            .OrderBy(t => t.Period)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Transaction>> GetRecentAsync(int limit, CancellationToken ct) =>
        await _db.Transactions
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken ct) =>
        await _db.Transactions
            .OrderByDescending(t => t.Period)
            .ThenByDescending(t => t.CreatedAt)
            .Take(500)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<Transaction> Items, int Total, decimal NetBalance)> GetPagedAsync(
        TransactionType? type, string? search, int page, int pageSize, CancellationToken ct)
    {
        // All DbSets carry the global tenant filter, so the join stays scoped to the current org.
        var query =
            from t in _db.Transactions
            join c in _db.Contracts on t.ContractId equals c.Id
            join at in _db.AppTenants on c.AppTenantId equals at.Id
            join p in _db.Properties on c.PropertyId equals p.Id
            select new { t, at, p };

        if (type.HasValue)
            query = query.Where(x => x.t.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.at.FirstName + " " + x.at.LastName).ToLower().Contains(s) ||
                x.p.Address.ToLower().Contains(s) ||
                (x.t.Notes ?? "").ToLower().Contains(s));
        }

        var total = await query.CountAsync(ct);

        // Cash model, consistent with the owner settlement and the tenant balance (audit A1): money in
        // (payments + credits) minus what's owed (charges, ignoring cancelled), over the WHOLE filtered
        // set — not just the page.
        var netBalance = await query.SumAsync(x =>
            x.t.Type == TransactionType.Payment || x.t.Type == TransactionType.ManualCredit
                ? x.t.Amount
                : x.t.Status == TransactionStatus.Cancelled
                    ? 0m
                    : -x.t.Amount,
            ct);

        var items = await query
            .OrderByDescending(x => x.t.Period)
            .ThenByDescending(x => x.t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.t)
            .ToListAsync(ct);

        return (items, total, netBalance);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken ct) =>
        await _db.Transactions.AddAsync(transaction, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
