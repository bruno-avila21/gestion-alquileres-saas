using GestionAlquileres.Application.Features.Owners.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Owners.Commands;

public class CreateOwnerCommandHandler : IRequestHandler<CreateOwnerCommand, OwnerDto>
{
    private readonly IOwnerRepository _repo;

    public CreateOwnerCommandHandler(IOwnerRepository repo) => _repo = repo;

    public async Task<OwnerDto> Handle(CreateOwnerCommand request, CancellationToken ct)
    {
        var owner = new Owner
        {
            OrganizationId = request.OrganizationId,
            Name = request.Name.Trim(),
            TaxId = request.TaxId?.Trim(),
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            Cbu = request.Cbu?.Trim(),
            Notes = request.Notes?.Trim(),
        };

        await _repo.AddAsync(owner, ct);
        await _repo.SaveChangesAsync(ct);

        return ToDto(owner);
    }

    internal static OwnerDto ToDto(Owner o) =>
        new(o.Id, o.OrganizationId, o.Name, o.TaxId, o.Email, o.Phone, o.Cbu, o.Notes, o.IsActive, o.CreatedAt);
}
