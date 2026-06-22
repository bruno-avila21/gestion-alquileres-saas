using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Properties.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Properties.Commands;

public class CreatePropertyCommandHandler : IRequestHandler<CreatePropertyCommand, PropertyDto>
{
    private readonly IPropertyRepository _repo;
    private readonly IOwnerRepository _ownerRepo;

    public CreatePropertyCommandHandler(IPropertyRepository repo, IOwnerRepository ownerRepo)
    {
        _repo = repo;
        _ownerRepo = ownerRepo;
    }

    public async Task<PropertyDto> Handle(CreatePropertyCommand request, CancellationToken ct)
    {
        // ExistsAsync is tenant-filtered, so an owner from another org reads as not found.
        if (request.OwnerId is { } ownerId && !await _ownerRepo.ExistsAsync(ownerId, ct))
            throw new BusinessException("El propietario indicado no existe.");

        var property = new Property
        {
            OrganizationId = request.OrganizationId,
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            Province = request.Province.Trim(),
            PropertyType = request.PropertyType,
            AreaM2 = request.AreaM2,
            Notes = request.Notes?.Trim(),
            OwnerId = request.OwnerId,
            CommissionPct = request.CommissionPct,
        };

        await _repo.AddAsync(property, ct);
        await _repo.SaveChangesAsync(ct);

        return ToDto(property);
    }

    internal static PropertyDto ToDto(Property p) =>
        new(p.Id, p.OrganizationId, p.Address, p.City, p.Province,
            p.PropertyType, p.AreaM2, p.Notes, p.OwnerId, p.CommissionPct, p.IsActive, p.CreatedAt);
}
