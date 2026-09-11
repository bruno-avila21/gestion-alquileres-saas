using System.Text;
using GestionAlquileres.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GestionAlquileres.Tests.Phase7;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "localstore_" + Guid.NewGuid().ToString("N"));

    private LocalFileStorageService Create(string? basePath) =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:BasePath"] = basePath })
            .Build());

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, recursive: true);
        GC.SuppressFinalize(this);
    }

    // Regresión (auditoría 2026-07-31): appsettings.json trae "BasePath": "". Un string vacío no es
    // null, así que el `??` no se activaba y Directory.CreateDirectory("") tiraba ArgumentException
    // al construir el servicio — el primer upload de un clone limpio se caía.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_BlankBasePath_FallsBackInsteadOfThrowing(string? basePath)
    {
        var ex = Record.Exception(() => Create(basePath));
        Assert.Null(ex);
    }

    [Fact]
    public async Task Upload_Download_Delete_RoundTrips()
    {
        var svc = Create(_tempRoot);
        const string body = "contenido del comprobante";

        await using var input = new MemoryStream(Encoding.UTF8.GetBytes(body));
        var key = await svc.UploadAsync(input, "recibo.pdf", "application/pdf", CancellationToken.None);
        Assert.EndsWith(".pdf", key);

        await using (var stream = await svc.DownloadAsync(key, CancellationToken.None))
        using (var reader = new StreamReader(stream))
            Assert.Equal(body, await reader.ReadToEndAsync());

        await svc.DeleteAsync(key, CancellationToken.None);
        await Assert.ThrowsAsync<FileNotFoundException>(() => svc.DownloadAsync(key, CancellationToken.None));
    }

    // Defensa en profundidad: hoy las claves son GUIDs generados por el propio servicio, pero
    // Path.Combine descarta la base si el segundo argumento es absoluto, y "../" la escaparía.
    [Fact]
    public async Task Download_KeyWithTraversal_IsRejected()
    {
        var svc = Create(_tempRoot);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.DownloadAsync(Path.Combine("..", "escapado.txt"), CancellationToken.None));
    }

    [Fact]
    public async Task Download_RootedKey_IsRejected()
    {
        var svc = Create(_tempRoot);
        var rooted = Path.Combine(Path.GetTempPath(), "afuera.txt");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.DownloadAsync(rooted, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_KeyWithTraversal_IsRejected()
    {
        var svc = Create(_tempRoot);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.DeleteAsync(Path.Combine("..", "escapado.txt"), CancellationToken.None));
    }
}
