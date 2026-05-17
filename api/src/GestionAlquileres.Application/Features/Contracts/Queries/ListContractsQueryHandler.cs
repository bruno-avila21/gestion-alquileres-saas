using GestionAlquileres.Application.Features.Contracts.Commands;
using GestionAlquileres.Application.Features.Contracts.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Contracts.Queries;

public class ListContractsQueryHandler : IRequestHandler<ListContractsQuery, IReadOnlyList<ContractDto>>
{
    private readonly IContractRepository _repo;

    public ListContractsQueryHandler(IContractRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<ContractDto>> Handle(ListContractsQuery request, CancellationToken ct)
    {
        var contracts = await _repo.ListAsync(request.AppTenantId, request.PropertyId, request.Status, ct);
        return contracts.Select(CreateContractCommandHandler.ToDto).ToList();
    }
}
