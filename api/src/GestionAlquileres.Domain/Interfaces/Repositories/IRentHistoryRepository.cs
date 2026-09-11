using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface IRentHistoryRepository
{
    Task<IReadOnlyList<RentHistory>> GetByContractAsync(Guid contractId, CancellationToken ct);
    Task<RentHistory?> GetLastByContractAsync(Guid contractId, CancellationToken ct);
    Task<IReadOnlyList<RentHistory>> GetAllAsync(CancellationToken ct);
    /// <summary>One page of the org's adjustments (optionally filtered by type and by tenant/address/notes), plus the total.</summary>
    Task<(IReadOnlyList<RentHistory> Items, int Total)> GetPagedAsync(
        AdjustmentType? type, string? search, int page, int pageSize, CancellationToken ct);
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

    /// <summary>
    /// Cuántos ajustes lleva el contrato. El scheduler lo usa para anclar la cadencia a
    /// <c>StartDate + k·frecuencia</c> en vez de encadenar desde la última fecha efectiva, que
    /// desplazaba el día en contratos que arrancan el 29, 30 o 31.
    /// Ignora el filtro de tenant: corre desde un job, sin organización en contexto.
    /// </summary>
    Task<int> CountByContractRawAsync(Guid contractId, CancellationToken ct);
}
