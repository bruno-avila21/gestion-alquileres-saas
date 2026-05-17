using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence.Repositories;

public class AppTenantRepository : IAppTenantRepository
{
    private readonly AppDbContext _db;
    public AppTenantRepository(AppDbContext db) => _db = db;

    public Task<AppTenant?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.AppTenants.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<AppTenant>> GetAllAsync(CancellationToken ct) =>
        await _db.AppTenants.OrderBy(t => t.LastName).ThenBy(t => t.FirstName).ToListAsync(ct);

    public async Task AddAsync(AppTenant tenant, CancellationToken ct) =>
        await _db.AppTenants.AddAsync(tenant, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct) =>
        _db.AppTenants.AnyAsync(t => t.Id == id, ct);

    public Task<bool> DniExistsAsync(string dni, CancellationToken ct) =>
        _db.AppTenants.AnyAsync(t => t.Dni == dni, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
