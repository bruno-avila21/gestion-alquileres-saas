using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GestionAlquileres.API.Jobs;

/// <summary>
/// Daily maintenance: purges expired refresh tokens so the table stays bounded (audit A-17).
/// A short grace window is kept past expiry for audit/reuse-detection before deletion.
/// </summary>
public class RefreshTokenCleanupJob
{
    private const int GraceDaysAfterExpiry = 2;

    private readonly IServiceProvider _sp;
    private readonly ILogger<RefreshTokenCleanupJob> _logger;

    public RefreshTokenCleanupJob(IServiceProvider sp, ILogger<RefreshTokenCleanupJob> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        await using var scope = _sp.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

        // Instant comparison — timezone is irrelevant here.
        var cutoff = DateTimeOffset.UtcNow.AddDays(-GraceDaysAfterExpiry);
        var removed = await repo.DeleteExpiredAsync(cutoff, ct);

        _logger.LogInformation("Refresh-token cleanup removed {Count} expired tokens (older than {Cutoff:u})",
            removed, cutoff);
    }
}
