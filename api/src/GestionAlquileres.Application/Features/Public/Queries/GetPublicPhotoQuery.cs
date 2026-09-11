using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Public.Queries;

public record PublicPhotoFile(Stream Content, string MimeType);

/// <summary>Foto de una ficha. Sólo se sirve si la propiedad tiene alguna publicación visible.</summary>
public record GetPublicPhotoQuery(Guid PhotoId) : IRequest<PublicPhotoFile?>;

public class GetPublicPhotoQueryHandler : IRequestHandler<GetPublicPhotoQuery, PublicPhotoFile?>
{
    private readonly IPropertyPhotoRepository _photos;
    private readonly IListingRepository _listings;
    private readonly IStorageService _storage;

    public GetPublicPhotoQueryHandler(IPropertyPhotoRepository photos, IListingRepository listings, IStorageService storage)
    {
        _photos = photos;
        _listings = listings;
        _storage = storage;
    }

    public async Task<PublicPhotoFile?> Handle(GetPublicPhotoQuery request, CancellationToken ct)
    {
        var photo = await _photos.GetByIdAsync(request.PhotoId, ct);
        if (photo is null) return null;

        if (!await _listings.ExistsPublishedForPropertyAsync(photo.PropertyId, ct)) return null;

        var stream = await _storage.DownloadAsync(photo.StorageKey, ct);
        return new PublicPhotoFile(stream, photo.MimeType);
    }
}
