using GestionAlquileres.Application.Features.RentHistory.Commands;
using GestionAlquileres.Application.Features.RentHistory.DTOs;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Me;

public class GetMyRentHistoryQueryHandler : IRequestHandler<GetMyRentHistoryQuery, IReadOnlyList<RentHistoryDto>>
{
    private readonly IAppTenantRepository _tenantRepo;
    private readonly IContractRepository _contractRepo;
    private readonly IRentHistoryRepository _historyRepo;

    public GetMyRentHistoryQueryHandler(
        IAppTenantRepository tenantRepo,
        IContractRepository contractRepo,
        IRentHistoryRepository historyRepo)
    {
        _tenantRepo = tenantRepo;
        _contractRepo = contractRepo;
        _historyRepo = historyRepo;
    }

    public async Task<IReadOnlyList<RentHistoryDto>> Handle(GetMyRentHistoryQuery request, CancellationToken ct)
    {
        var appTenant = await _tenantRepo.GetByUserIdAsync(request.UserId, ct);
        if (appTenant is null) return [];

        var contracts = await _contractRepo.ListAsync(appTenant.Id, null, ContractStatus.Active, ct);
        var contract = contracts.FirstOrDefault();
        if (contract is null) return [];

        var records = await _historyRepo.GetByContractAsync(contract.Id, ct);
        return records.Select(ApplyRentAdjustmentCommandHandler.ToDto).ToList();
    }
}
