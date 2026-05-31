using GestionAlquileres.Application.Features.Contracts.Commands;
using GestionAlquileres.Application.Features.Contracts.DTOs;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Me;

public class GetMyContractQueryHandler : IRequestHandler<GetMyContractQuery, ContractDto?>
{
    private readonly IAppTenantRepository _tenantRepo;
    private readonly IContractRepository _contractRepo;

    public GetMyContractQueryHandler(IAppTenantRepository tenantRepo, IContractRepository contractRepo)
    {
        _tenantRepo = tenantRepo;
        _contractRepo = contractRepo;
    }

    public async Task<ContractDto?> Handle(GetMyContractQuery request, CancellationToken ct)
    {
        var appTenant = await _tenantRepo.GetByUserIdAsync(request.UserId, ct);
        if (appTenant is null) return null;

        var contracts = await _contractRepo.ListAsync(appTenant.Id, null, ContractStatus.Active, ct);
        var contract = contracts.FirstOrDefault();
        return contract is null ? null : CreateContractCommandHandler.ToDto(contract);
    }
}
