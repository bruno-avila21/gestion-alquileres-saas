using FluentValidation;
using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.PropertyPhotos.Commands;

public record DeletePropertyPhotoCommand(Guid PropertyId, Guid PhotoId) : IRequest;

public class DeletePropertyPhotoCommandValidator : AbstractValidator<DeletePropertyPhotoCommand>
{
    public DeletePropertyPhotoCommandValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.PhotoId).NotEmpty();
    }
}

public class DeletePropertyPhotoCommandHandler : IRequestHandler<DeletePropertyPhotoCommand>
{
    private readonly IPropertyPhotoRepository _photos;
    private readonly IStorageService _storage;

    public DeletePropertyPhotoCommandHandler(IPropertyPhotoRepository photos, IStorageService storage)
    {
        _photos = photos;
        _storage = storage;
    }

    public async Task Handle(DeletePropertyPhotoCommand request, CancellationToken ct)
    {
        var photo = await _photos.GetByIdAsync(request.PhotoId, ct);
        if (photo is null || photo.PropertyId != request.PropertyId)
            throw new BusinessException("La foto no existe.");

        _photos.Remove(photo);

        // If the cover goes, promote the next one so the public card never shows a blank.
        if (photo.IsCover)
        {
            var next = (await _photos.GetByPropertyAsync(request.PropertyId, ct)).FirstOrDefault(p => p.Id != photo.Id);
            if (next is not null) next.IsCover = true;
        }

        // DB first, storage after: an orphan file is harmless, a dangling row is a broken image.
        await _photos.SaveChangesAsync(ct);
        await _storage.DeleteAsync(photo.StorageKey, ct);
    }
}
