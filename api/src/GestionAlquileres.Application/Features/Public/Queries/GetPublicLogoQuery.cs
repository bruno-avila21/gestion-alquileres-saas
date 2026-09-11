using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Public.Queries;

/// <summary>
/// Logo de la inmobiliaria para su sitio público. Es el mismo archivo que encabeza los recibos
/// (<c>Organization.LogoStorageKey</c>), servido por un endpoint anónimo en vez de por
/// <c>GET /organization/logo</c>, que exige token del panel.
///
/// No rompe la regla de "documentos privados con URL pre-firmada": un logo institucional es
/// justamente lo que la inmobiliaria quiere que se vea. Se sirve con la clave del storage
/// resuelta en el servidor —nunca expuesta al cliente— y sólo para una organización activa.
/// </summary>
public record GetPublicLogoQuery(string Slug) : IRequest<PublicPhotoFile?>;

public class GetPublicLogoQueryHandler : IRequestHandler<GetPublicLogoQuery, PublicPhotoFile?>
{
    private readonly IOrganizationRepository _orgs;
    private readonly IStorageService _storage;

    public GetPublicLogoQueryHandler(IOrganizationRepository orgs, IStorageService storage)
    {
        _orgs = orgs;
        _storage = storage;
    }

    public async Task<PublicPhotoFile?> Handle(GetPublicLogoQuery request, CancellationToken ct)
    {
        var org = await _orgs.GetBySlugAsync(request.Slug.ToLowerInvariant(), ct);
        // Una organización suspendida apaga su sitio entero, logo incluido.
        if (org is null || !org.IsActive || string.IsNullOrWhiteSpace(org.LogoStorageKey)) return null;

        var stream = await _storage.DownloadAsync(org.LogoStorageKey, ct);
        return new PublicPhotoFile(stream, MimeTypeFromKey(org.LogoStorageKey));
    }

    // Organization no guarda el mime type del logo aparte: el storage conserva la extensión
    // original al generar la clave, así que alcanza con mirarla (igual que en
    // GetOrganizationLogoQueryHandler, el equivalente autenticado del panel).
    private static string MimeTypeFromKey(string storageKey) =>
        Path.GetExtension(storageKey).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream",
        };
}
