using FluentValidation;
using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.PropertyPhotos.Commands;

public record SetCoverPhotoCommand(Guid PropertyId, Guid PhotoId) : IRequest;

public class SetCoverPhotoCommandValidator : AbstractValidator<SetCoverPhotoCommand>
{
    public SetCoverPhotoCommandValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.PhotoId).NotEmpty();
    }
}

public class SetCoverPhotoCommandHandler : IRequestHandler<SetCoverPhotoCommand>
{
    private readonly IPropertyPhotoRepository _photos;
    public SetCoverPhotoCommandHandler(IPropertyPhotoRepository photos) => _photos = photos;

    public async Task Handle(SetCoverPhotoCommand request, CancellationToken ct)
    {
        var photos = await _photos.GetByPropertyAsync(request.PropertyId, ct);
        var target = photos.FirstOrDefault(p => p.Id == request.PhotoId)
            ?? throw new BusinessException("La foto no existe.");

        foreach (var p in photos) p.IsCover = p.Id == target.Id;
        await _photos.SaveChangesAsync(ct);
    }
}
