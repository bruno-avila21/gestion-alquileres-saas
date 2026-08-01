using GestionAlquileres.Application.Common.Time;
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

    public Task<bool> HasActiveOverlapAsync(
        Guid propertyId, DateOnly startDate, DateOnly endDate, Guid? excludeContractId, CancellationToken ct) =>
        _db.Contracts
            .Where(c => c.PropertyId == propertyId && c.Status == ContractStatus.Active)
            .Where(c => excludeContractId == null || c.Id != excludeContractId.Value)
            // Dos rangos [s1,e1] y [s2,e2] se solapan sii  s1 <= e2  &&  s2 <= e1.
            .Where(c => startDate <= c.EndDate && c.StartDate <= endDate)
            .AnyAsync(ct);

    public async Task AddAsync(Contract contract, CancellationToken ct) =>
        await _db.Contracts.AddAsync(contract, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    public Task<Contract?> GetByIdRawAsync(Guid id, Guid organizationId, CancellationToken ct) =>
        _db.Contracts
            .IgnoreQueryFilters()
            .Include(c => c.Property)
            .Include(c => c.AppTenant)
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId, ct);

    public async Task<IReadOnlyList<Contract>> ListActiveRawAsync(CancellationToken ct) =>
        await _db.Contracts
            .IgnoreQueryFilters()
            .Include(c => c.Property)
            .Include(c => c.AppTenant)
            .Where(c => c.Status == ContractStatus.Active)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Contract>> GetExpiringRawAsync(int daysAhead, CancellationToken ct)
    {
        // Hora argentina, no UTC: entre las 21:00 y las 24:00 locales UtcNow ya está en el día
        // siguiente, y un contrato que vence hoy quedaría fuera de la ventana (auditoría B-20).
        var today = ArgentinaTime.Today;
        var cutoff = today.AddDays(daysAhead);
        return await _db.Contracts
            .IgnoreQueryFilters()
            .Include(c => c.Property)
            .Include(c => c.AppTenant)
            .Where(c => c.Status == ContractStatus.Active && c.EndDate >= today && c.EndDate <= cutoff)
            .ToListAsync(ct);
    }
}
