using GestionAlquileres.Application.Features.Owners.Commands;
using GestionAlquileres.Application.Features.Owners.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Owners.Queries;

public class GetOwnerByIdQueryHandler : IRequestHandler<GetOwnerByIdQuery, OwnerDto?>
{
    private readonly IOwnerRepository _repo;

    public GetOwnerByIdQueryHandler(IOwnerRepository repo) => _repo = repo;

    public async Task<OwnerDto?> Handle(GetOwnerByIdQuery request, CancellationToken ct)
    {
        var owner = await _repo.GetByIdAsync(request.Id, ct);
        return owner is null ? null : CreateOwnerCommandHandler.ToDto(owner);
    }
}
