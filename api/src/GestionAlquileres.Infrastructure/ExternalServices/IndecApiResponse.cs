using System.Text.Json;
using System.Text.Json.Serialization;

namespace GestionAlquileres.Infrastructure.ExternalServices;

/// <summary>Public contract returned by IndecApiClient.</summary>
public record IndecDataPoint(DateOnly Fecha, decimal Valor);

// datos.gob.ar Series API returns data as array-of-arrays:
//   { "data": [ ["2024-01-01", 4500.5], ["2024-02-01", 4650.2] ], "count": 2, "meta": [...] }
// RESEARCH Pitfall 5 — must deserialize as List<JsonElement[]> and project manually.
internal record IndecRawResponse(
    [property: JsonPropertyName("data")] List<JsonElement[]>? Data,
    [property: JsonPropertyName("count")] int Count
);
