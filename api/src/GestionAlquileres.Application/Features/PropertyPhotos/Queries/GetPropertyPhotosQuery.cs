using GestionAlquileres.Application.Features.PropertyPhotos.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.PropertyPhotos.Queries;

public record GetPropertyPhotosQuery(Guid PropertyId) : IRequest<IReadOnlyList<PropertyPhotoDto>>;

public class GetPropertyPhotosQueryHandler : IRequestHandler<GetPropertyPhotosQuery, IReadOnlyList<PropertyPhotoDto>>
{
    private readonly IPropertyPhotoRepository _photos;
    private readonly IOrganizationRepository _orgs;
    private readonly ICurrentTenant _currentTenant;

    public GetPropertyPhotosQueryHandler(IPropertyPhotoRepository photos, IOrganizationRepository orgs, ICurrentTenant currentTenant)
    {
        _photos = photos;
        _orgs = orgs;
        _currentTenant = currentTenant;
    }

    public async Task<IReadOnlyList<PropertyPhotoDto>> Handle(GetPropertyPhotosQuery request, CancellationToken ct)
    {
        var org = await _orgs.GetByIdAsync(_currentTenant.OrganizationId, ct);
        var slug = org?.Slug ?? "";
        return (await _photos.GetByPropertyAsync(request.PropertyId, ct)).Select(p => PropertyPhotoDto.From(p, slug)).ToList();
    }
}
