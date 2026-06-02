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

    // Background-job helpers — bypass the global tenant filter (safe only in Hangfire jobs, never HTTP handlers).
    // The raw by-id lookup still pins the organization explicitly so it can never cross tenants.
    Task<Contract?> GetByIdRawAsync(Guid id, Guid organizationId, CancellationToken ct);
    Task<IReadOnlyList<Contract>> ListActiveRawAsync(CancellationToken ct);
    Task<IReadOnlyList<Contract>> GetExpiringRawAsync(int daysAhead, CancellationToken ct);
}
