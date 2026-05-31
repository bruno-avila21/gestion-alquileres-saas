namespace GestionAlquileres.Application.Common.Settings;

/// <summary>Connection to the standalone indices-api (rent-index source + calculation service).</summary>
public class IndicesApiSettings
{
    public const string SectionName = "IndicesApi";

    public string BaseUrl { get; set; } = "http://localhost:5000";
    public string? ApiKey { get; set; }
}
