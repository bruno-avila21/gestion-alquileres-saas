using GestionAlquileres.Application.Features.Owners.Commands;
using GestionAlquileres.Application.Features.Owners.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Owners.Queries;

public class GetOwnersQueryHandler : IRequestHandler<GetOwnersQuery, IReadOnlyList<OwnerDto>>
{
    private readonly IOwnerRepository _repo;

    public GetOwnersQueryHandler(IOwnerRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<OwnerDto>> Handle(GetOwnersQuery request, CancellationToken ct)
    {
        var owners = await _repo.GetAllAsync(ct);
        return owners.Select(CreateOwnerCommandHandler.ToDto).ToList();
    }
}
