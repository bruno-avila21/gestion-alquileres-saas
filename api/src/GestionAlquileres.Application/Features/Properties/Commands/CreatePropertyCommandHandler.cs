using GestionAlquileres.Application.Features.Properties.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Properties.Commands;

public class CreatePropertyCommandHandler : IRequestHandler<CreatePropertyCommand, PropertyDto>
{
    private readonly IPropertyRepository _repo;

    public CreatePropertyCommandHandler(IPropertyRepository repo) => _repo = repo;

    public async Task<PropertyDto> Handle(CreatePropertyCommand request, CancellationToken ct)
    {
        var property = new Property
        {
            OrganizationId = request.OrganizationId,
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            Province = request.Province.Trim(),
            PropertyType = request.PropertyType,
            AreaM2 = request.AreaM2,
            Notes = request.Notes?.Trim(),
        };

        await _repo.AddAsync(property, ct);
        await _repo.SaveChangesAsync(ct);

        return ToDto(property);
    }

    internal static PropertyDto ToDto(Property p) =>
        new(p.Id, p.OrganizationId, p.Address, p.City, p.Province,
            p.PropertyType, p.AreaM2, p.Notes, p.IsActive, p.CreatedAt);
}
