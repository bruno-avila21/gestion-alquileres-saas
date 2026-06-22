using GestionAlquileres.Domain.Entities;
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

    public async Task AddAsync(Transaction transaction, CancellationToken ct) =>
        await _db.Transactions.AddAsync(transaction, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
