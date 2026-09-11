using GestionAlquileres.Application.Features.Organizations.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Organizations.Queries;

public class GetOrganizationLogoQueryHandler : IRequestHandler<GetOrganizationLogoQuery, OrganizationLogoDto?>
{
    private readonly IOrganizationRepository _repo;
    private readonly IStorageService _storage;
    private readonly ICurrentTenant _currentTenant;

    public GetOrganizationLogoQueryHandler(
        IOrganizationRepository repo, IStorageService storage, ICurrentTenant currentTenant)
    {
        _repo = repo;
        _storage = storage;
        _currentTenant = currentTenant;
    }

    public async Task<OrganizationLogoDto?> Handle(GetOrganizationLogoQuery request, CancellationToken ct)
    {
        var org = await _repo.GetByIdAsync(_currentTenant.OrganizationId, ct)
            ?? throw new InvalidOperationException("La organización del token no existe.");

        if (string.IsNullOrWhiteSpace(org.LogoStorageKey))
            return null;

        await using var stream = await _storage.DownloadAsync(org.LogoStorageKey, ct);
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, ct);

        return new OrganizationLogoDto(memory.ToArray(), MimeTypeFromKey(org.LogoStorageKey));
    }

    // El modelo de Organization no guarda un campo de mime type aparte (el contrato de la marca sólo
    // define LogoStorageKey): el storage local/S3 conserva la extensión original al generar la clave
    // (ver LocalFileStorageService/S3StorageService.UploadAsync), así que alcanza con mirarla.
    private static string MimeTypeFromKey(string storageKey) =>
        Path.GetExtension(storageKey).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream",
        };
}
