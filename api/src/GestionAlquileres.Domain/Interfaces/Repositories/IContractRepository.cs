using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface IContractRepository
{
    Task<Contract?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Contract>> ListAsync(Guid? appTenantId, Guid? propertyId, ContractStatus? status, CancellationToken ct);
    Task AddAsync(Contract contract, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
