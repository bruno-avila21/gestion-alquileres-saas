using System.Globalization;
using System.Net.Http.Json;
using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace GestionAlquileres.Infrastructure.ExternalServices;

public class BcraApiClient : IBcraApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BcraApiClient> _logger;

    // BCRA variable ID for ICL (Índice para Contratos de Locación).
    // Verified via community code — re-confirm via /estadisticas/v1/principalesvariables if API changes.
    public const int IclVariableId = 7988;

    public BcraApiClient(HttpClient httpClient, ILogger<BcraApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BcraIndexPoint>> GetIclAsync(
        DateOnly desde, DateOnly hasta, CancellationToken ct = default)
    {
        // v1 path — confirmed by multiple community implementations.
        var url = $"/estadisticas/v1/datosvariable/{IclVariableId}" +
                  $"/{desde:yyyy-MM-dd}/{hasta:yyyy-MM-dd}";

        _logger.LogInformation("Fetching BCRA ICL from {Url}", url);

        // Use GetAsync + EnsureSuccessStatusCode so HttpRequestException propagates on 5xx
        // (enables SyncIndexCommandHandler fallback path per IDX-04).
        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadFromJsonAsync<BcraRawResponse>(cancellationToken: ct);
        if (raw?.Results is null || raw.Results.Count == 0)
            return Array.Empty<BcraIndexPoint>();

        var result = new List<BcraIndexPoint>(raw.Results.Count);
        foreach (var point in raw.Results)
        {
            // RESEARCH Pitfall 1: dd/MM/yyyy, not MM/dd/yyyy.
            var fecha = DateOnly.ParseExact(point.Fecha, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            result.Add(new BcraIndexPoint(fecha, point.Valor));
        }
        return result;
    }
}
