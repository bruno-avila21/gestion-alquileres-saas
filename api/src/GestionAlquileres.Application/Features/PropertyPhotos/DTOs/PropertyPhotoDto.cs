using GestionAlquileres.Application.Common;
using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Application.Features.PropertyPhotos.DTOs;

public record PropertyPhotoDto(
    Guid Id,
    Guid PropertyId,
    string Url,
    string MimeType,
    long SizeBytes,
    int SortOrder,
    bool IsCover,
    DateTimeOffset CreatedAt)
{
    public static PropertyPhotoDto From(PropertyPhoto p, string orgSlug) =>
        new(p.Id, p.PropertyId, PublicUrls.Photo(orgSlug, p.Id), p.MimeType, p.SizeBytes, p.SortOrder, p.IsCover, p.CreatedAt);
}
