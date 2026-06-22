using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Transaction>> GetByContractAsync(Guid contractId, CancellationToken ct);
    Task<IReadOnlyList<Transaction>> GetRecentAsync(int limit, CancellationToken ct);
    Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken ct);
    Task AddAsync(Transaction transaction, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
