using System.Text.Json.Serialization;

namespace GestionAlquileres.Infrastructure.ExternalServices;

// Raw shape from api.bcra.gob.ar — note fecha is DD/MM/YYYY string (RESEARCH Pitfall 1).
// Public point type is BcraIndexPoint defined in GestionAlquileres.Domain.Interfaces.Services.
internal record BcraRawResponse(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("results")] List<BcraRawDataPoint>? Results
);

internal record BcraRawDataPoint(
    [property: JsonPropertyName("fecha")] string Fecha,
    [property: JsonPropertyName("valor")] decimal Valor
);
