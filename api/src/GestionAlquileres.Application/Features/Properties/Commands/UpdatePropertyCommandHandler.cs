using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Properties.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Properties.Commands;

public class UpdatePropertyCommandHandler : IRequestHandler<UpdatePropertyCommand, PropertyDto>
{
    private readonly IPropertyRepository _repo;

    public UpdatePropertyCommandHandler(IPropertyRepository repo) => _repo = repo;

    public async Task<PropertyDto> Handle(UpdatePropertyCommand request, CancellationToken ct)
    {
        var property = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new BusinessException($"Property {request.Id} not found.");

        property.Address = request.Address.Trim();
        property.City = request.City.Trim();
        property.Province = request.Province.Trim();
        property.PropertyType = request.PropertyType;
        property.AreaM2 = request.AreaM2;
        property.Notes = request.Notes?.Trim();
        property.IsActive = request.IsActive;

        await _repo.SaveChangesAsync(ct);

        return CreatePropertyCommandHandler.ToDto(property);
    }
}
