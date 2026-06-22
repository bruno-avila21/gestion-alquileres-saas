using GestionAlquileres.Application.Features.Transactions.Commands;
using GestionAlquileres.Application.Features.Transactions.DTOs;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Me;

public class GetMyTransactionsQueryHandler : IRequestHandler<GetMyTransactionsQuery, IReadOnlyList<TransactionDto>>
{
    private readonly IAppTenantRepository _tenantRepo;
    private readonly IContractRepository _contractRepo;
    private readonly ITransactionRepository _txRepo;

    public GetMyTransactionsQueryHandler(
        IAppTenantRepository tenantRepo,
        IContractRepository contractRepo,
        ITransactionRepository txRepo)
    {
        _tenantRepo = tenantRepo;
        _contractRepo = contractRepo;
        _txRepo = txRepo;
    }

    public async Task<IReadOnlyList<TransactionDto>> Handle(GetMyTransactionsQuery request, CancellationToken ct)
    {
        var appTenant = await _tenantRepo.GetByUserIdAsync(request.UserId, ct);
        if (appTenant is null) return [];

        var contracts = await _contractRepo.ListAsync(appTenant.Id, null, ContractStatus.Active, ct);
        var contract = contracts.FirstOrDefault();
        if (contract is null) return [];

        var txs = await _txRepo.GetByContractAsync(contract.Id, ct);
        return txs.Select(RegisterPaymentCommandHandler.ToDto).ToList();
    }
}
