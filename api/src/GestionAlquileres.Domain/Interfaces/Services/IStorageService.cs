namespace GestionAlquileres.Domain.Interfaces.Services;

public interface IStorageService
{
    Task<string> UploadAsync(Stream content, string fileName, string mimeType, CancellationToken ct);
    Task<Stream> DownloadAsync(string storageKey, CancellationToken ct);
    Task DeleteAsync(string storageKey, CancellationToken ct);
}
