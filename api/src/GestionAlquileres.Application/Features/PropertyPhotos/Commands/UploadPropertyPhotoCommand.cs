using FluentValidation;
using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.PropertyPhotos.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.PropertyPhotos.Commands;

public record UploadPropertyPhotoCommand(
    Guid PropertyId,
    string FileName,
    string MimeType,
    long SizeBytes,
    Stream Content)
    : IRequest<PropertyPhotoDto>;

public class UploadPropertyPhotoCommandValidator : AbstractValidator<UploadPropertyPhotoCommand>
{
    public const long MaxSizeBytes = 10 * 1024 * 1024; // 10 MB
    public const int MaxPhotosPerProperty = 40;

    // Only raster images: SVG is active content and would be served back to any visitor.
    public static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp",
    };

    public UploadPropertyPhotoCommandValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.MimeType).NotEmpty()
            .Must(m => AllowedMimeTypes.Contains(m))
            .WithMessage("Formato no permitido. Aceptados: JPG, PNG y WebP.");
        RuleFor(x => x.SizeBytes).GreaterThan(0).LessThanOrEqualTo(MaxSizeBytes)
            .WithMessage("Cada foto puede pesar hasta 10 MB.");
        RuleFor(x => x.Content).NotNull();
    }
}

public class UploadPropertyPhotoCommandHandler : IRequestHandler<UploadPropertyPhotoCommand, PropertyPhotoDto>
{
    private readonly IPropertyPhotoRepository _photos;
    private readonly IPropertyRepository _properties;
    private readonly IOrganizationRepository _orgs;
    private readonly IStorageService _storage;
    private readonly ICurrentTenant _currentTenant;

    public UploadPropertyPhotoCommandHandler(
        IPropertyPhotoRepository photos,
        IPropertyRepository properties,
        IOrganizationRepository orgs,
        IStorageService storage,
        ICurrentTenant currentTenant)
    {
        _photos = photos;
        _properties = properties;
        _orgs = orgs;
        _storage = storage;
        _currentTenant = currentTenant;
    }

    public async Task<PropertyPhotoDto> Handle(UploadPropertyPhotoCommand request, CancellationToken ct)
    {
        if (!await _properties.ExistsAsync(request.PropertyId, ct))
            throw new BusinessException("La propiedad indicada no existe.");

        var count = await _photos.CountByPropertyAsync(request.PropertyId, ct);
        if (count >= UploadPropertyPhotoCommandValidator.MaxPhotosPerProperty)
            throw new BusinessException($"Una propiedad admite hasta {UploadPropertyPhotoCommandValidator.MaxPhotosPerProperty} fotos.");

        var storageKey = await _storage.UploadAsync(request.Content, request.FileName, request.MimeType, ct);

        var photo = new PropertyPhoto
        {
            OrganizationId = _currentTenant.OrganizationId,
            PropertyId = request.PropertyId,
            StorageKey = storageKey,
            MimeType = request.MimeType,
            SizeBytes = request.SizeBytes,
            SortOrder = count,
            IsCover = count == 0, // the first photo is the cover until someone picks another
        };

        await _photos.AddAsync(photo, ct);
        await _photos.SaveChangesAsync(ct);

        var org = await _orgs.GetByIdAsync(_currentTenant.OrganizationId, ct);
        return PropertyPhotoDto.From(photo, org?.Slug ?? "");
    }
}
