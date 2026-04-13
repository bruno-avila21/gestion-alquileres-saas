using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Indexes.Commands;
using GestionAlquileres.Application.Features.Indexes.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace GestionAlquileres.Tests.Phase2.Application;

// ---------------------------------------------------------------------------
// Hand-rolled stubs — no Moq dependency
// ---------------------------------------------------------------------------

internal sealed class StubIndexRepoSync : IIndexRepository
{
    public Dictionary<(IndexType, DateOnly), IndexValue> Store = new();
    public IndexValue? NextLastAvailable;
    public IndexValue? AddedValue;
    public bool SaveCalled;

    public Task<IndexValue?> GetByPeriodAsync(IndexType t, DateOnly p, CancellationToken ct) =>
        Task.FromResult(Store.TryGetValue((t, p), out var v) ? v : null);

    public Task<IndexValue?> GetLastAvailableAsync(IndexType t, CancellationToken ct) =>
        Task.FromResult(NextLastAvailable);

    public Task<IReadOnlyList<IndexValue>> GetRangeAsync(IndexType type, DateOnly from, DateOnly to, CancellationToken ct)
    {
        IReadOnlyList<IndexValue> list = Store.Values
            .Where(v => v.IndexType == type && v.Period >= from && v.Period <= to)
            .OrderBy(v => v.Period)
            .ToList();
        return Task.FromResult(list);
    }

    public Task AddAsync(IndexValue v, CancellationToken ct)
    {
        AddedValue = v;
        Store[(v.IndexType, v.Period)] = v;
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(IndexType t, DateOnly p, CancellationToken ct) =>
        Task.FromResult(Store.ContainsKey((t, p)));

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveCalled = true;
        return Task.CompletedTask;
    }
}

internal sealed class StubBcraClient : IBcraApiClient
{
    public Func<DateOnly, DateOnly, IReadOnlyList<BcraIndexPoint>>? Responder;
    public Exception? ToThrow;

    public Task<IReadOnlyList<BcraIndexPoint>> GetIclAsync(DateOnly d, DateOnly h, CancellationToken ct = default)
    {
        if (ToThrow is not null) throw ToThrow;
        return Task.FromResult(Responder?.Invoke(d, h) ?? (IReadOnlyList<BcraIndexPoint>)Array.Empty<BcraIndexPoint>());
    }
}

internal sealed class StubIndecClient : IIndecApiClient
{
    public Func<DateOnly, DateOnly, IReadOnlyList<IndecIndexPoint>>? Responder;
    public Exception? ToThrow;

    public Task<IReadOnlyList<IndecIndexPoint>> GetIpcAsync(DateOnly d, DateOnly h, CancellationToken ct = default)
    {
        if (ToThrow is not null) throw ToThrow;
        return Task.FromResult(Responder?.Invoke(d, h) ?? (IReadOnlyList<IndecIndexPoint>)Array.Empty<IndecIndexPoint>());
    }
}

internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message, Exception? Ex)> Logs = new();
    public IDisposable? BeginScope<TState>(TState s) where TState : notnull => NullScope.Instance;
    public bool IsEnabled(LogLevel l) => true;
    public void Log<TState>(LogLevel l, EventId id, TState s, Exception? ex, Func<TState, Exception?, string> f)
        => Logs.Add((l, f(s, ex), ex));

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

public class SyncIndexCommandHandlerTests
{
    private static SyncIndexCommandHandler BuildHandler(
        StubIndexRepoSync repo,
        StubBcraClient bcra,
        StubIndecClient indec,
        ILogger<SyncIndexCommandHandler>? logger = null)
        => new(repo, bcra, indec, logger ?? new CapturingLogger<SyncIndexCommandHandler>());

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task T1_Sync_ICL_happy_path_persists_normalized_period()
    {
        var repo = new StubIndexRepoSync();
        var bcra = new StubBcraClient
        {
            Responder = (_, _) => new List<BcraIndexPoint>
            {
                new(new DateOnly(2024, 3, 15), 100m),
                new(new DateOnly(2024, 3, 28), 102m),
                new(new DateOnly(2024, 3, 29), 103m),
            }
        };
        var handler = BuildHandler(repo, bcra, new StubIndecClient());

        var result = await handler.Handle(new SyncIndexCommand(IndexType.ICL, new DateOnly(2024, 3, 1)), default);

        Assert.True(result.Success);
        Assert.False(result.WasFallback);
        Assert.False(result.AlreadyExisted);
        Assert.NotNull(repo.AddedValue);
        Assert.Equal(new DateOnly(2024, 3, 1), repo.AddedValue!.Period);
        Assert.Equal(103m, repo.AddedValue.Value);
        Assert.Equal("BCRA", repo.AddedValue.Source);
        Assert.Equal(IndexType.ICL, repo.AddedValue.IndexType);
        Assert.True(repo.SaveCalled);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task T2_Sync_ICL_when_already_exists_returns_AlreadySynced_without_calling_API()
    {
        var repo = new StubIndexRepoSync();
        var existing = new IndexValue
        {
            Id = Guid.NewGuid(),
            IndexType = IndexType.ICL,
            Period = new DateOnly(2024, 3, 1),
            Value = 100m,
            Source = "BCRA",
            FetchedAt = DateTimeOffset.UtcNow
        };
        repo.Store[(IndexType.ICL, new DateOnly(2024, 3, 1))] = existing;
        var bcra = new StubBcraClient { ToThrow = new Exception("should-not-be-called") };
        var handler = BuildHandler(repo, bcra, new StubIndecClient());

        var result = await handler.Handle(new SyncIndexCommand(IndexType.ICL, new DateOnly(2024, 3, 1)), default);

        Assert.True(result.AlreadyExisted);
        Assert.Null(repo.AddedValue);
        Assert.False(repo.SaveCalled);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task T3_Sync_ICL_when_API_fails_falls_back_to_last_available()
    {
        var repo = new StubIndexRepoSync
        {
            NextLastAvailable = new IndexValue
            {
                Id = Guid.NewGuid(),
                IndexType = IndexType.ICL,
                Period = new DateOnly(2024, 2, 1),
                Value = 99m,
                Source = "BCRA",
                FetchedAt = DateTimeOffset.UtcNow.AddDays(-30)
            }
        };
        var bcra = new StubBcraClient { ToThrow = new HttpRequestException("BCRA down") };
        var logger = new CapturingLogger<SyncIndexCommandHandler>();
        var handler = BuildHandler(repo, bcra, new StubIndecClient(), logger);

        var result = await handler.Handle(new SyncIndexCommand(IndexType.ICL, new DateOnly(2024, 3, 1)), default);

        Assert.True(result.Success);
        Assert.True(result.WasFallback);
        Assert.Equal(new DateOnly(2024, 2, 1), result.IndexValue.Period);
        Assert.Null(repo.AddedValue);
        Assert.True(logger.Logs.Any(l => l.Level == LogLevel.Warning));
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task T4_Sync_ICL_when_API_fails_and_no_fallback_available_throws_BusinessException()
    {
        var repo = new StubIndexRepoSync { NextLastAvailable = null };
        var bcra = new StubBcraClient { ToThrow = new HttpRequestException("BCRA down") };
        var handler = BuildHandler(repo, bcra, new StubIndecClient());

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => handler.Handle(new SyncIndexCommand(IndexType.ICL, new DateOnly(2024, 3, 1)), default));

        Assert.Contains("no hay valor previo disponible", ex.Message);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task T5_Sync_ICL_when_API_returns_empty_falls_back_or_throws_BusinessException()
    {
        // Empty result = API failure for this period → fallback path → no fallback → throws
        var repo = new StubIndexRepoSync { NextLastAvailable = null };
        var bcra = new StubBcraClient { Responder = (_, _) => Array.Empty<BcraIndexPoint>() };
        var handler = BuildHandler(repo, bcra, new StubIndecClient());

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => handler.Handle(new SyncIndexCommand(IndexType.ICL, new DateOnly(2024, 3, 1)), default));

        Assert.NotNull(ex.Message);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task T6_Sync_IPC_happy_path_source_is_INDEC()
    {
        var repo = new StubIndexRepoSync();
        var indec = new StubIndecClient
        {
            Responder = (_, _) => new List<IndecIndexPoint>
            {
                new(new DateOnly(2024, 3, 1), 4800m),
            }
        };
        var handler = BuildHandler(repo, new StubBcraClient(), indec);

        var result = await handler.Handle(new SyncIndexCommand(IndexType.IPC, new DateOnly(2024, 3, 1)), default);

        Assert.True(result.Success);
        Assert.False(result.WasFallback);
        Assert.Equal("INDEC", repo.AddedValue!.Source);
        Assert.Equal(IndexType.IPC, repo.AddedValue.IndexType);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task T7_Sync_normalizes_daily_values_to_month_start()
    {
        var repo = new StubIndexRepoSync();
        var bcra = new StubBcraClient
        {
            Responder = (_, _) => new List<BcraIndexPoint>
            {
                new(new DateOnly(2024, 3, 28), 101m),
            }
        };
        var handler = BuildHandler(repo, bcra, new StubIndecClient());

        await handler.Handle(new SyncIndexCommand(IndexType.ICL, new DateOnly(2024, 3, 1)), default);

        Assert.Equal(new DateOnly(2024, 3, 1), repo.AddedValue!.Period);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task T8_Sync_ICL_requests_correct_date_range_from_BCRA()
    {
        var repo = new StubIndexRepoSync();
        DateOnly capturedDesde = default, capturedHasta = default;
        var bcra = new StubBcraClient
        {
            Responder = (d, h) =>
            {
                capturedDesde = d;
                capturedHasta = h;
                return new List<BcraIndexPoint> { new(new DateOnly(2024, 3, 15), 100m) };
            }
        };
        var handler = BuildHandler(repo, bcra, new StubIndecClient());

        await handler.Handle(new SyncIndexCommand(IndexType.ICL, new DateOnly(2024, 3, 1)), default);

        Assert.Equal(new DateOnly(2024, 3, 1), capturedDesde);
        Assert.Equal(new DateOnly(2024, 3, 31), capturedHasta);
    }
}

// ---------------------------------------------------------------------------
// Validator tests
// ---------------------------------------------------------------------------

public class SyncIndexCommandValidatorTests
{
    [Fact]
    [Trait("Phase", "Phase2")]
    public void Validator_rejects_zero_IndexType()
    {
        var validator = new SyncIndexCommandValidator();
        var cmd = new SyncIndexCommand((IndexType)0, new DateOnly(2024, 3, 1));
        var result = validator.Validate(cmd);
        Assert.False(result.IsValid);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public void Validator_rejects_future_Period()
    {
        var validator = new SyncIndexCommandValidator();
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        var cmd = new SyncIndexCommand(IndexType.ICL, future);
        var result = validator.Validate(cmd);
        Assert.False(result.IsValid);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public void Validator_accepts_valid_command()
    {
        var validator = new SyncIndexCommandValidator();
        var cmd = new SyncIndexCommand(IndexType.ICL, new DateOnly(2024, 3, 1));
        var result = validator.Validate(cmd);
        Assert.True(result.IsValid);
    }
}
