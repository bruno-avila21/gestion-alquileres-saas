using GestionAlquileres.Application.Common.Time;
using GestionAlquileres.Application.Features.Indexes.Commands;
using GestionAlquileres.Domain.Enums;
using Hangfire;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GestionAlquileres.API.Jobs;

[DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
public class SyncIndexesJob
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<SyncIndexesJob> _logger;

    public SyncIndexesJob(IServiceProvider sp, ILogger<SyncIndexesJob> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        await using var scope = _sp.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var currentMonth = new DateOnly(ArgentinaTime.Today.Year, ArgentinaTime.Today.Month, 1);
        // INDEC publishes IPC with a ~6-week lag, so the current month is usually still empty when
        // the job runs. Sync a trailing window instead of just current+previous (audit M4): the ICL
        // adjustment reads a base index 12 months back, so those historical periods must exist or the
        // adjustment fails with "índice base no disponible" — which is exactly what happens on a fresh
        // deploy. Backfilling BackfillMonths covers the ICL/annual base plus margin. The sync is
        // idempotent — already-persisted periods are skipped (audit C-5) — so after the first backfill
        // only the one or two still-missing recent months hit the external API.
        const int BackfillMonths = 13;
        var periods = Enumerable.Range(0, BackfillMonths + 1)
            .Select(i => currentMonth.AddMonths(-i))
            .ToArray();

        foreach (var indexType in new[] { IndexType.ICL, IndexType.IPC })
        {
            foreach (var period in periods)
            {
                try
                {
                    var result = await mediator.Send(new SyncIndexCommand(indexType, period), ct);
                    var status = result.AlreadyExisted ? "already-existed" : result.WasFallback ? "fallback" : "synced";
                    _logger.LogInformation(
                        "Index sync {IndexType} {Period}: {Status}",
                        indexType, period.ToString("yyyy-MM"), status);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to sync {IndexType} index for {Period}",
                        indexType, period.ToString("yyyy-MM"));
                }
            }
        }
    }
}
