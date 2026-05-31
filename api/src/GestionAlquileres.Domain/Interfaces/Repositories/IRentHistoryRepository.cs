using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface IRentHistoryRepository
{
    Task<IReadOnlyList<RentHistory>> GetByContractAsync(Guid contractId, CancellationToken ct);
    Task<RentHistory?> GetLastByContractAsync(Guid contractId, CancellationToken ct);
    Task<IReadOnlyList<RentHistory>> GetAllAsync(CancellationToken ct);
    Task AddAsync(RentHistory record, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);

    /// <summary>
    /// True if an adjustment already exists for this contract on the given effective date.
    /// Used as an idempotency pre-check (the DB also enforces a unique (ContractId, EffectiveDate) index).
    /// Bypasses the tenant filter so it works in background jobs too — safe because the
    /// caller has already authorized access to <paramref name="contractId"/>.
    /// </summary>
    Task<bool> ExistsForPeriodAsync(Guid contractId, DateOnly effectiveDate, CancellationToken ct);

    // Background-job helper — bypass tenant filter
    Task<RentHistory?> GetLastByContractRawAsync(Guid contractId, CancellationToken ct);
}
