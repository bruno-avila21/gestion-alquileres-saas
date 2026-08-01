using Amazon.S3;
using Amazon.S3.Model;
using GestionAlquileres.Application.Common.Settings;
using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace GestionAlquileres.Infrastructure.Storage;

/// <summary>
/// Stores documents in an S3-compatible object store (AWS S3 or MinIO). Replaces the local
/// filesystem so the API can scale horizontally and survive restarts (no node-local state).
/// </summary>
public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;

    public S3StorageService(IAmazonS3 s3, IOptions<StorageSettings> settings)
    {
        _s3 = s3;
        _bucket = settings.Value.Bucket
            ?? throw new InvalidOperationException("Storage:Bucket is required when Storage:Provider is S3.");
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string mimeType, CancellationToken ct)
    {
        var storageKey = Guid.NewGuid().ToString("N") + Path.GetExtension(fileName);
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = storageKey,
            InputStream = content,
            ContentType = mimeType,
            AutoCloseStream = false,
        };
        await _s3.PutObjectAsync(request, ct);
        return storageKey;
    }

    public async Task<Stream> DownloadAsync(string storageKey, CancellationToken ct)
    {
        try
        {
            var response = await _s3.GetObjectAsync(_bucket, storageKey, ct);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException($"Storage key not found: {storageKey}");
        }
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct) =>
        _s3.DeleteObjectAsync(_bucket, storageKey, ct);
}
