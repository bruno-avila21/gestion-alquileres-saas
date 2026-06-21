using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;
    public RefreshTokenRepository(AppDbContext db) => _db = db;

    // RefreshToken has no global query filter (refresh runs without a tenant), so plain queries are safe.
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct) =>
        _db.Set<RefreshToken>().FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct) =>
        await _db.Set<RefreshToken>().AddAsync(token, ct);

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var active = await _db.Set<RefreshToken>()
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var t in active) t.RevokedAt = now;
    }

    public async Task<int> DeleteExpiredAsync(DateTimeOffset cutoff, CancellationToken ct)
    {
        var expired = await _db.Set<RefreshToken>()
            .Where(t => t.ExpiresAt < cutoff)
            .ToListAsync(ct);
        if (expired.Count == 0) return 0;

        _db.Set<RefreshToken>().RemoveRange(expired);
        await _db.SaveChangesAsync(ct);
        return expired.Count;
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
