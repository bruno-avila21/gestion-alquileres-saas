using System.Text.Json.Serialization;

namespace GestionAlquileres.Infrastructure.ExternalServices;

/// <summary>Public contract returned by BcraApiClient (domain-neutral).</summary>
public record BcraDataPoint(DateOnly Fecha, decimal Valor);

// Raw shape from api.bcra.gob.ar — note fecha is DD/MM/YYYY string (RESEARCH Pitfall 1).
internal record BcraRawResponse(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("results")] List<BcraRawDataPoint>? Results
);

internal record BcraRawDataPoint(
    [property: JsonPropertyName("fecha")] string Fecha,
    [property: JsonPropertyName("valor")] decimal Valor
);
