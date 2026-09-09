using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace GestionAlquileres.Infrastructure.Storage;

/// <summary>
/// Filesystem-backed storage. Development only: it lives on a single node's disk, is not shared
/// between instances and does not survive a container restart. <c>SecuritySettingsValidator</c>
/// refuses to start the application with <c>Storage:Provider=Local</c> outside Development.
/// </summary>
public class LocalFileStorageService : IStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        var configured = configuration["Storage:BasePath"];

        // appsettings.json ships "BasePath": "" — an empty string is NOT null, so a plain `??`
        // fallback never fires and Directory.CreateDirectory("") throws ArgumentException on the
        // first upload. Treat blank as "unset".
        _basePath = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Directory.GetCurrentDirectory(), "uploads")
            : Path.GetFullPath(configured);

        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string mimeType, CancellationToken ct)
    {
        var storageKey = Guid.NewGuid().ToString("N") + Path.GetExtension(fileName);
        var filePath = ResolvePath(storageKey);
        await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(fs, ct);
        return storageKey;
    }

    public Task<Stream> DownloadAsync(string storageKey, CancellationToken ct)
    {
        var filePath = ResolvePath(storageKey);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Storage key not found: {storageKey}");
        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        var filePath = ResolvePath(storageKey);
        if (File.Exists(filePath)) File.Delete(filePath);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Resolves a storage key inside the base directory. Defense in depth: keys are generated as
    /// GUIDs so they are not attacker-controlled today, but Path.Combine silently discards the base
    /// path when the second argument is rooted, and "../" segments would otherwise escape it.
    /// </summary>
    private string ResolvePath(string storageKey)
    {
        var candidate = Path.GetFullPath(Path.Combine(_basePath, storageKey));
        var root = _basePath.EndsWith(Path.DirectorySeparatorChar)
            ? _basePath
            : _basePath + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(root, StringComparison.Ordinal))
            throw new UnauthorizedAccessException($"Storage key escapes the storage root: {storageKey}");

        return candidate;
    }
}
