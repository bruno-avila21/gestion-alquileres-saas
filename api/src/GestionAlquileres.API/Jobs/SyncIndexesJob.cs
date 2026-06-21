using GestionAlquileres.Application.Common.Time;
using GestionAlquileres.Application.Features.Indexes.Commands;
using GestionAlquileres.Domain.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GestionAlquileres.API.Jobs;

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
        // the job runs. Also (re)sync the previous month to pick up values that weren't available
        // last run. The sync is idempotent — already-persisted periods are skipped (audit C-5).
        var periods = new[] { currentMonth, currentMonth.AddMonths(-1) };

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
