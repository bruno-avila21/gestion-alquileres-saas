using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Owners.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Owners.Commands;

public class UpdateOwnerCommandHandler : IRequestHandler<UpdateOwnerCommand, OwnerDto>
{
    private readonly IOwnerRepository _repo;

    public UpdateOwnerCommandHandler(IOwnerRepository repo) => _repo = repo;

    public async Task<OwnerDto> Handle(UpdateOwnerCommand request, CancellationToken ct)
    {
        var owner = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new BusinessException($"Propietario {request.Id} no encontrado.");

        owner.Name = request.Name.Trim();
        owner.TaxId = request.TaxId?.Trim();
        owner.Email = request.Email?.Trim();
        owner.Phone = request.Phone?.Trim();
        owner.Cbu = request.Cbu?.Trim();
        owner.Notes = request.Notes?.Trim();
        owner.IsActive = request.IsActive;

        await _repo.SaveChangesAsync(ct);

        return CreateOwnerCommandHandler.ToDto(owner);
    }
}
