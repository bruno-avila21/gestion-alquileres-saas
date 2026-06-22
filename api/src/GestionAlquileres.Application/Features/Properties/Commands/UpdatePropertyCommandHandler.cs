using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Properties.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Properties.Commands;

public class UpdatePropertyCommandHandler : IRequestHandler<UpdatePropertyCommand, PropertyDto>
{
    private readonly IPropertyRepository _repo;
    private readonly IOwnerRepository _ownerRepo;

    public UpdatePropertyCommandHandler(IPropertyRepository repo, IOwnerRepository ownerRepo)
    {
        _repo = repo;
        _ownerRepo = ownerRepo;
    }

    public async Task<PropertyDto> Handle(UpdatePropertyCommand request, CancellationToken ct)
    {
        var property = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new BusinessException($"Property {request.Id} not found.");

        if (request.OwnerId is { } ownerId && !await _ownerRepo.ExistsAsync(ownerId, ct))
            throw new BusinessException("El propietario indicado no existe.");

        property.Address = request.Address.Trim();
        property.City = request.City.Trim();
        property.Province = request.Province.Trim();
        property.PropertyType = request.PropertyType;
        property.AreaM2 = request.AreaM2;
        property.Notes = request.Notes?.Trim();
        property.OwnerId = request.OwnerId;
        property.CommissionPct = request.CommissionPct;
        property.IsActive = request.IsActive;

        await _repo.SaveChangesAsync(ct);

        return CreatePropertyCommandHandler.ToDto(property);
    }
}
