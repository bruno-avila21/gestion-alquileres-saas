using System.Net.Http.Json;
using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace GestionAlquileres.Infrastructure.ExternalServices;

/// <summary>
/// Talks to the standalone indices-api instead of hitting BCRA/INDEC directly. Implements both
/// source-client interfaces by calling indices-api's idempotent sync endpoint, which fetches and
/// normalizes the monthly value. The SaaS still persists the value locally (its own cache).
/// </summary>
public class IndicesApiClient : IBcraApiClient, IIndecApiClient, IIndicesCalculator
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IndicesApiClient> _logger;

    public IndicesApiClient(HttpClient httpClient, ILogger<IndicesApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BcraIndexPoint>> GetIclAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default)
    {
        var value = await SyncAsync("ICL", hasta, ct);
        return value is null ? Array.Empty<BcraIndexPoint>() : new[] { new BcraIndexPoint(hasta, value.Value) };
    }

    public async Task<IReadOnlyList<IndecIndexPoint>> GetIpcAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default)
    {
        var value = await SyncAsync("IPC", hasta, ct);
        return value is null ? Array.Empty<IndecIndexPoint>() : new[] { new IndecIndexPoint(hasta, value.Value) };
    }

    public async Task<AdjustmentProjection?> CalculateAsync(
        string index, decimal initialRent, DateOnly startDate, int frequencyMonths, DateOnly? until,
        CancellationToken ct = default)
    {
        var body = new
        {
            index,
            initialRent,
            startDate = startDate.ToString("yyyy-MM-dd"),
            frequencyMonths,
            until = until?.ToString("yyyy-MM-dd"),
        };

        var response = await _httpClient.PostAsJsonAsync("/v1/calculate", body, ct);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<CalcResponse>(cancellationToken: ct);
        if (dto is null) return null;

        var schedule = (dto.Schedule ?? new List<CalcItem>())
            .Select(s => new AdjustmentProjectionItem(
                s.Number, s.From, s.To, s.Rent, s.Coefficient, s.VariationPct, s.IndexAvailable))
            .ToList();
        return new AdjustmentProjection(dto.CurrentRent, schedule, dto.Notes);
    }

    private async Task<decimal?> SyncAsync(string type, DateOnly month, CancellationToken ct)
    {
        var period = month.ToString("yyyy-MM");
        _logger.LogInformation("Requesting {Type} {Period} from indices-api", type, period);

        var response = await _httpClient.PostAsJsonAsync($"/v1/indices/{type}/sync", new { period }, ct);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<SyncResponse>(cancellationToken: ct);
        return dto?.Value?.Value;
    }

    private sealed record SyncResponse(SyncValue? Value);
    private sealed record SyncValue(decimal Value);

    private sealed record CalcResponse(decimal CurrentRent, List<CalcItem>? Schedule, string? Notes);
    private sealed record CalcItem(
        int Number, DateOnly From, DateOnly To, decimal? Rent, decimal? Coefficient, decimal? VariationPct, bool IndexAvailable);
}
