using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface IContractRepository
{
    Task<Contract?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Contract>> ListAsync(Guid? appTenantId, Guid? propertyId, ContractStatus? status, CancellationToken ct);

    /// <summary>
    /// True si la propiedad ya tiene un contrato Activo cuyo rango de fechas se solapa con [startDate, endDate].
    /// Tenant-scoped (respeta el filtro global). <paramref name="excludeContractId"/> excluye el contrato propio al editar.
    /// </summary>
    Task<bool> HasActiveOverlapAsync(Guid propertyId, DateOnly startDate, DateOnly endDate, Guid? excludeContractId, CancellationToken ct);

    Task AddAsync(Contract contract, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);

    // Background-job helpers — bypass tenant filter (safe only in Hangfire jobs, never HTTP handlers)
    Task<Contract?> GetByIdRawAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Contract>> ListActiveRawAsync(CancellationToken ct);
    Task<IReadOnlyList<Contract>> GetExpiringRawAsync(int daysAhead, CancellationToken ct);
}
