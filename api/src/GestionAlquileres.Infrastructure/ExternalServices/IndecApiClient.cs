using System.Globalization;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace GestionAlquileres.Infrastructure.ExternalServices;

public class IndecApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IndecApiClient> _logger;

    // Series ID for IPC Nivel General (base diciembre 2016 = 100).
    public const string IpcSeriesId = "148.3_INIVELNAL_DICI_M_26";

    public IndecApiClient(HttpClient httpClient, ILogger<IndecApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<IndecDataPoint>> GetIpcAsync(
        DateOnly desde, DateOnly hasta, CancellationToken ct = default)
    {
        var url = $"/series/api/series/?ids={IpcSeriesId}" +
                  $"&start_date={desde:yyyy-MM-dd}&end_date={hasta:yyyy-MM-dd}&limit=1000";

        _logger.LogInformation("Fetching INDEC IPC from {Url}", url);

        // Use GetAsync + EnsureSuccessStatusCode so HttpRequestException propagates on 5xx
        // (enables SyncIndexCommandHandler fallback path per IDX-04).
        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadFromJsonAsync<IndecRawResponse>(cancellationToken: ct);
        if (raw?.Data is null || raw.Data.Count == 0)
            return Array.Empty<IndecDataPoint>();

        var result = new List<IndecDataPoint>(raw.Data.Count);
        foreach (var row in raw.Data)
        {
            if (row.Length < 2) continue;
            var dateStr = row[0].GetString();
            if (string.IsNullOrWhiteSpace(dateStr)) continue;
            var fecha = DateOnly.ParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var valor = row[1].GetDecimal();
            result.Add(new IndecDataPoint(fecha, valor));
        }
        return result;
    }
}
