using GestionAlquileres.Application.Features.Contracts.Commands;
using GestionAlquileres.Application.Features.Contracts.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Contracts.Queries;

public class GetContractByIdQueryHandler : IRequestHandler<GetContractByIdQuery, ContractDto?>
{
    private readonly IContractRepository _repo;

    public GetContractByIdQueryHandler(IContractRepository repo) => _repo = repo;

    public async Task<ContractDto?> Handle(GetContractByIdQuery request, CancellationToken ct)
    {
        var contract = await _repo.GetByIdAsync(request.Id, ct);
        return contract is null ? null : CreateContractCommandHandler.ToDto(contract);
    }
}
