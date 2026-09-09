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
        // Sólo la consumen query handlers: ninguno muta las entidades devueltas, así que no hace
        // falta el seguimiento de cambios ni sus snapshots.
        var q = _db.Contracts
            .AsNoTracking()
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

    public async Task<(int ActiveCount, decimal MonthlyRevenue, int ExpiringCount)> GetDashboardStatsAsync(
        DateOnly today, DateOnly until, CancellationToken ct)
    {
        // Un solo round-trip: se agrupa por una constante para que los tres agregados salgan en la
        // misma consulta, sin traer una sola fila de contrato.
        var stats = await _db.Contracts
            .AsNoTracking()
            .Where(c => c.Status == ContractStatus.Active)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                ActiveCount = g.Count(),
                MonthlyRevenue = g.Sum(c => c.MonthlyRent),
                // "Por vencer" = termina dentro de los próximos 30 días. Acotado por abajo para que
                // los ya vencidos no inflen el número (auditoría C-9).
                ExpiringCount = g.Count(c => c.EndDate >= today && c.EndDate <= until),
            })
            .FirstOrDefaultAsync(ct);

        return stats is null
            ? (0, 0m, 0)
            : (stats.ActiveCount, stats.MonthlyRevenue, stats.ExpiringCount);
    }

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
