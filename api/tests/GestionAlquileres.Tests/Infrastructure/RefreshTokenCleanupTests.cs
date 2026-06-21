using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Services;
using GestionAlquileres.Infrastructure.Persistence;
using GestionAlquileres.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Tests.Infrastructure;

/// <summary>Audit A-17: expired refresh tokens are purged; active ones are kept.</summary>
public class RefreshTokenCleanupTests
{
    private class StubTenant : ICurrentTenant
    {
        public Guid OrganizationId => Guid.Empty;
    }

    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options, new StubTenant());

    [Fact]
    public async Task DeleteExpiredAsync_removes_only_tokens_past_the_cutoff()
    {
        using var db = CreateDb();
        var repo = new RefreshTokenRepository(db);
        var now = DateTimeOffset.UtcNow;

        var expired1 = new RefreshToken { UserId = Guid.NewGuid(), TokenHash = "a", ExpiresAt = now.AddDays(-10) };
        var expired2 = new RefreshToken { UserId = Guid.NewGuid(), TokenHash = "b", ExpiresAt = now.AddDays(-5) };
        var active = new RefreshToken { UserId = Guid.NewGuid(), TokenHash = "c", ExpiresAt = now.AddDays(7) };

        await repo.AddAsync(expired1, CancellationToken.None);
        await repo.AddAsync(expired2, CancellationToken.None);
        await repo.AddAsync(active, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var removed = await repo.DeleteExpiredAsync(now.AddDays(-2), CancellationToken.None);

        Assert.Equal(2, removed);
        var remaining = await db.Set<RefreshToken>().ToListAsync();
        Assert.Single(remaining);
        Assert.Equal("c", remaining[0].TokenHash);
    }

    [Fact]
    public async Task DeleteExpiredAsync_returns_zero_when_nothing_expired()
    {
        using var db = CreateDb();
        var repo = new RefreshTokenRepository(db);
        var now = DateTimeOffset.UtcNow;

        await repo.AddAsync(new RefreshToken { UserId = Guid.NewGuid(), TokenHash = "x", ExpiresAt = now.AddDays(10) }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var removed = await repo.DeleteExpiredAsync(now.AddDays(-2), CancellationToken.None);

        Assert.Equal(0, removed);
        Assert.Single(await db.Set<RefreshToken>().ToListAsync());
    }
}
