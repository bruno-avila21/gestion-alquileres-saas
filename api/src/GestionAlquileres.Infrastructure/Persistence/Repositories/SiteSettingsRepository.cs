using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence.Repositories;

public class SiteSettingsRepository : ISiteSettingsRepository
{
    private readonly AppDbContext _db;
    public SiteSettingsRepository(AppDbContext db) => _db = db;

    // Sin IgnoreQueryFilters: el filtro global ya acota a la organización del token, y en el
    // sitio público a la que TenantMiddleware resolvió desde el slug.
    public Task<SiteSettings?> GetAsync(CancellationToken ct) =>
        _db.SiteSettings.FirstOrDefaultAsync(ct);

    public async Task AddAsync(SiteSettings settings, CancellationToken ct) =>
        await _db.SiteSettings.AddAsync(settings, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
