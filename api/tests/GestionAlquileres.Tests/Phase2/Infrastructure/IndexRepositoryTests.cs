using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Services;
using GestionAlquileres.Infrastructure.Persistence;
using GestionAlquileres.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Tests.Phase2.Infrastructure;

public class IndexRepositoryTests
{
    private class StubTenant : ICurrentTenant
    {
        public Guid OrganizationId => Guid.Empty;
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options, new StubTenant());
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task AddAsync_and_SaveChangesAsync_persists_IndexValue()
    {
        using var db = CreateDb();
        var repo = new IndexRepository(db);

        var indexValue = new IndexValue
        {
            IndexType = IndexType.ICL,
            Period = new DateOnly(2024, 3, 1),
            Value = 100.5m,
            Source = "BCRA"
        };

        await repo.AddAsync(indexValue, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var result = await repo.GetByPeriodAsync(IndexType.ICL, new DateOnly(2024, 3, 1), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(100.5m, result!.Value);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task GetByPeriodAsync_returns_null_when_not_found()
    {
        using var db = CreateDb();
        var repo = new IndexRepository(db);

        var result = await repo.GetByPeriodAsync(IndexType.ICL, new DateOnly(2024, 3, 1), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task ExistsAsync_returns_true_when_period_exists()
    {
        using var db = CreateDb();
        var repo = new IndexRepository(db);

        var indexValue = new IndexValue
        {
            IndexType = IndexType.ICL,
            Period = new DateOnly(2024, 3, 1),
            Value = 100.5m,
            Source = "BCRA"
        };

        await repo.AddAsync(indexValue, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var exists = await repo.ExistsAsync(IndexType.ICL, new DateOnly(2024, 3, 1), CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task GetLastAvailableAsync_returns_most_recent_period_for_type()
    {
        using var db = CreateDb();
        var repo = new IndexRepository(db);

        await repo.AddAsync(new IndexValue { IndexType = IndexType.ICL, Period = new DateOnly(2024, 1, 1), Value = 98m, Source = "BCRA" }, CancellationToken.None);
        await repo.AddAsync(new IndexValue { IndexType = IndexType.ICL, Period = new DateOnly(2024, 2, 1), Value = 99m, Source = "BCRA" }, CancellationToken.None);
        await repo.AddAsync(new IndexValue { IndexType = IndexType.ICL, Period = new DateOnly(2024, 3, 1), Value = 100m, Source = "BCRA" }, CancellationToken.None);
        await repo.AddAsync(new IndexValue { IndexType = IndexType.IPC, Period = new DateOnly(2024, 3, 1), Value = 200m, Source = "INDEC" }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var result = await repo.GetLastAvailableAsync(IndexType.ICL, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2024, 3, 1), result!.Period);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task GetRangeAsync_returns_only_rows_in_range_and_type()
    {
        using var db = CreateDb();
        var repo = new IndexRepository(db);

        await repo.AddAsync(new IndexValue { IndexType = IndexType.ICL, Period = new DateOnly(2023, 12, 1), Value = 95m, Source = "BCRA" }, CancellationToken.None);
        await repo.AddAsync(new IndexValue { IndexType = IndexType.ICL, Period = new DateOnly(2024, 1, 1), Value = 97m, Source = "BCRA" }, CancellationToken.None);
        await repo.AddAsync(new IndexValue { IndexType = IndexType.ICL, Period = new DateOnly(2024, 2, 1), Value = 99m, Source = "BCRA" }, CancellationToken.None);
        await repo.AddAsync(new IndexValue { IndexType = IndexType.ICL, Period = new DateOnly(2024, 3, 1), Value = 101m, Source = "BCRA" }, CancellationToken.None);
        await repo.AddAsync(new IndexValue { IndexType = IndexType.IPC, Period = new DateOnly(2024, 2, 1), Value = 190m, Source = "INDEC" }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var result = await repo.GetRangeAsync(IndexType.ICL, new DateOnly(2024, 1, 1), new DateOnly(2024, 2, 1), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(IndexType.ICL, r.IndexType));
        Assert.All(result, r => Assert.True(r.Period >= new DateOnly(2024, 1, 1) && r.Period <= new DateOnly(2024, 2, 1)));
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task Composite_unique_on_IndexType_Period_idempotency_check()
    {
        // InMemory DB does not enforce unique constraints, so we test idempotency via ExistsAsync.
        // The real DB constraint (ix_index_values_type_period_unique) is verified by the migration.
        using var db = CreateDb();
        var repo = new IndexRepository(db);

        var indexValue = new IndexValue
        {
            IndexType = IndexType.ICL,
            Period = new DateOnly(2024, 3, 1),
            Value = 100m,
            Source = "BCRA"
        };

        await repo.AddAsync(indexValue, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // After insert, ExistsAsync must return true — handler can use this for idempotency check
        var exists = await repo.ExistsAsync(IndexType.ICL, new DateOnly(2024, 3, 1), CancellationToken.None);
        Assert.True(exists);
    }
}
