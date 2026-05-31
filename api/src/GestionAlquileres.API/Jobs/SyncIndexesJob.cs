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

        var period = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var indexType in new[] { IndexType.ICL, IndexType.IPC })
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
