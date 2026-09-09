using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence.Repositories;

public class ListingRepository : IListingRepository
{
    private readonly AppDbContext _db;
    public ListingRepository(AppDbContext db) => _db = db;

    public Task<Listing?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Listings.Include(l => l.Property).FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<IReadOnlyList<Listing>> GetAllAsync(Guid? propertyId, CancellationToken ct)
    {
        var query = _db.Listings.AsNoTracking().Include(l => l.Property).AsQueryable();
        if (propertyId is { } pid) query = query.Where(l => l.PropertyId == pid);
        return await query.OrderByDescending(l => l.UpdatedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Listing>> GetPublishedAsync(CancellationToken ct) =>
        await _db.Listings.AsNoTracking()
            .Include(l => l.Property)
            .Where(l => l.Status == ListingStatus.Published && l.Property.IsActive)
            .ToListAsync(ct);

    public Task<bool> ExistsPublishedForPropertyAsync(Guid propertyId, CancellationToken ct) =>
        _db.Listings.AnyAsync(l => l.PropertyId == propertyId && l.Status == ListingStatus.Published, ct);

    public async Task AddAsync(Listing listing, CancellationToken ct) =>
        await _db.Listings.AddAsync(listing, ct);

    public void Remove(Listing listing) => _db.Listings.Remove(listing);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
