namespace GestionAlquileres.Application.Common.Settings;

/// <summary>
/// Document storage configuration. Provider "Local" uses the filesystem (dev); "S3" uses any
/// S3-compatible object store (AWS S3 or MinIO) — set ServiceUrl + ForcePathStyle for MinIO.
/// </summary>
public class StorageSettings
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "Local"; // "Local" | "S3"

    // Local
    public string? BasePath { get; set; }

    // S3 / MinIO
    public string? Bucket { get; set; }
    public string? ServiceUrl { get; set; }   // e.g. http://localhost:9000 for MinIO; null for AWS
    public string? Region { get; set; } = "us-east-1";
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public bool ForcePathStyle { get; set; } = true; // required by MinIO
}
