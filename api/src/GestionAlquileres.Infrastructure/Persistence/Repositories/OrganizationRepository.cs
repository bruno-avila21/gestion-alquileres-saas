using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly AppDbContext _db;
    public OrganizationRepository(AppDbContext db) => _db = db;

    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Organizations.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Organization?> GetBySlugAsync(string slug, CancellationToken ct) =>
        _db.Organizations.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Slug == slug, ct);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct) =>
        _db.Organizations.IgnoreQueryFilters().AnyAsync(o => o.Slug == slug, ct);

    public async Task AddAsync(Organization org, CancellationToken ct)
    {
        await _db.Organizations.AddAsync(org, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
