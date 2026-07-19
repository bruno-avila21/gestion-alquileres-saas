using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct);
    /// <summary>
    /// One page of the org's transactions (optionally filtered by type and tenant/address/notes), the
    /// total count, and the net cash balance over the WHOLE filtered set (credits − owed charges).
    /// </summary>
    Task<(IReadOnlyList<Transaction> Items, int Total, decimal NetBalance)> GetPagedAsync(
        TransactionType? type, string? search, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<Transaction>> GetByContractAsync(Guid contractId, CancellationToken ct);
    /// <summary>Pending charges (RentCharge/ManualDebit, Status=Pending) of a contract, oldest first — for payment allocation.</summary>
    Task<IReadOnlyList<Transaction>> GetPendingChargesAsync(Guid contractId, CancellationToken ct);
    Task<IReadOnlyList<Transaction>> GetRecentAsync(int limit, CancellationToken ct);
    Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken ct);
    Task AddAsync(Transaction transaction, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
