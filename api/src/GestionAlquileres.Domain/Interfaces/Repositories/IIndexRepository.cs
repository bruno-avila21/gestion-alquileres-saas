using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface IIndexRepository
{
    Task<IndexValue?> GetByPeriodAsync(IndexType type, DateOnly period, CancellationToken ct);
    Task<IndexValue?> GetLastAvailableAsync(IndexType type, CancellationToken ct);
    Task<IReadOnlyList<IndexValue>> GetRangeAsync(IndexType type, DateOnly from, DateOnly to, CancellationToken ct);
    Task AddAsync(IndexValue indexValue, CancellationToken ct);
    Task<bool> ExistsAsync(IndexType type, DateOnly period, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
