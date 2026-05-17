using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence.Repositories;

public class ContractRepository : IContractRepository
{
    private readonly AppDbContext _db;
    public ContractRepository(AppDbContext db) => _db = db;

    public Task<Contract?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Contracts
            .Include(c => c.Property)
            .Include(c => c.AppTenant)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Contract>> ListAsync(
        Guid? appTenantId, Guid? propertyId, ContractStatus? status, CancellationToken ct)
    {
        var q = _db.Contracts
            .Include(c => c.Property)
            .Include(c => c.AppTenant)
            .AsQueryable();

        if (appTenantId.HasValue) q = q.Where(c => c.AppTenantId == appTenantId.Value);
        if (propertyId.HasValue)  q = q.Where(c => c.PropertyId  == propertyId.Value);
        if (status.HasValue)      q = q.Where(c => c.Status       == status.Value);

        return await q.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);
    }

    public async Task AddAsync(Contract contract, CancellationToken ct) =>
        await _db.Contracts.AddAsync(contract, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
