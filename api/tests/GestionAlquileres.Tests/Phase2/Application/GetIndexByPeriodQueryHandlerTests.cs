using GestionAlquileres.Application.Features.Indexes.DTOs;
using GestionAlquileres.Application.Features.Indexes.Queries;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;

namespace GestionAlquileres.Tests.Phase2.Application;

// ---------------------------------------------------------------------------
// Hand-rolled stub
// ---------------------------------------------------------------------------

internal sealed class StubIndexRepoQuery : IIndexRepository
{
    public List<IndexValue> AllValues = new();

    public Task<IndexValue?> GetByPeriodAsync(IndexType t, DateOnly p, CancellationToken ct) =>
        Task.FromResult(AllValues.FirstOrDefault(v => v.IndexType == t && v.Period == p));

    public Task<IndexValue?> GetLastAvailableAsync(IndexType t, CancellationToken ct) =>
        Task.FromResult(AllValues.Where(v => v.IndexType == t).OrderByDescending(v => v.Period).FirstOrDefault());

    public Task<IReadOnlyList<IndexValue>> GetRangeAsync(IndexType type, DateOnly from, DateOnly to, CancellationToken ct)
    {
        IReadOnlyList<IndexValue> list = AllValues
            .Where(v => v.IndexType == type && v.Period >= from && v.Period <= to)
            .OrderBy(v => v.Period)
            .ToList();
        return Task.FromResult(list);
    }

    public Task AddAsync(IndexValue v, CancellationToken ct) { AllValues.Add(v); return Task.CompletedTask; }

    public Task<bool> ExistsAsync(IndexType t, DateOnly p, CancellationToken ct) =>
        Task.FromResult(AllValues.Any(v => v.IndexType == t && v.Period == p));

    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

public class GetIndexByPeriodQueryHandlerTests
{
    private static IndexValue MakeValue(IndexType type, DateOnly period, decimal value, string source) =>
        new()
        {
            Id = Guid.NewGuid(),
            IndexType = type,
            Period = period,
            Value = value,
            Source = source,
            FetchedAt = DateTimeOffset.UtcNow
        };

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task T1_Query_returns_rows_from_repo_mapped_to_DTO()
    {
        var repo = new StubIndexRepoQuery();
        repo.AllValues.Add(MakeValue(IndexType.ICL, new DateOnly(2024, 1, 1), 100m, "BCRA"));
        repo.AllValues.Add(MakeValue(IndexType.ICL, new DateOnly(2024, 6, 1), 110m, "BCRA"));

        var handler = new GetIndexByPeriodQueryHandler(repo);
        var result = await handler.Handle(
            new GetIndexByPeriodQuery(IndexType.ICL, new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)),
            default);

        Assert.Equal(2, result.Count);
        Assert.Equal(new DateOnly(2024, 1, 1), result[0].Period);
        Assert.Equal(100m, result[0].Value);
        Assert.Equal(IndexType.ICL, result[0].IndexType);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task T2_Query_returns_empty_list_when_no_rows_in_range()
    {
        var repo = new StubIndexRepoQuery();
        var handler = new GetIndexByPeriodQueryHandler(repo);

        var result = await handler.Handle(
            new GetIndexByPeriodQuery(IndexType.ICL, new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)),
            default);

        Assert.Empty(result);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task T3_Query_does_not_return_other_index_types()
    {
        var repo = new StubIndexRepoQuery();
        repo.AllValues.Add(MakeValue(IndexType.ICL, new DateOnly(2024, 3, 1), 100m, "BCRA"));
        repo.AllValues.Add(MakeValue(IndexType.IPC, new DateOnly(2024, 3, 1), 4500m, "INDEC"));

        var handler = new GetIndexByPeriodQueryHandler(repo);
        var result = await handler.Handle(
            new GetIndexByPeriodQuery(IndexType.ICL, new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)),
            default);

        Assert.Single(result);
        Assert.All(result, r => Assert.Equal(IndexType.ICL, r.IndexType));
    }
}

// ---------------------------------------------------------------------------
// Validator tests
// ---------------------------------------------------------------------------

public class GetIndexByPeriodQueryValidatorTests
{
    [Fact]
    [Trait("Phase", "Phase2")]
    public void Validator_rejects_inverted_range()
    {
        var validator = new GetIndexByPeriodQueryValidator();
        var query = new GetIndexByPeriodQuery(IndexType.ICL, new DateOnly(2024, 12, 1), new DateOnly(2024, 1, 1));
        var result = validator.Validate(query);
        Assert.False(result.IsValid);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public void Validator_accepts_valid_query()
    {
        var validator = new GetIndexByPeriodQueryValidator();
        var query = new GetIndexByPeriodQuery(IndexType.ICL, new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));
        var result = validator.Validate(query);
        Assert.True(result.IsValid);
    }
}
