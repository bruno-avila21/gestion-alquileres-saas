using GestionAlquileres.Application.Common.Time;
using GestionAlquileres.Application.Features.Transactions.DTOs;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Dashboard;

public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly IContractRepository _contractRepo;
    private readonly ITransactionRepository _txRepo;

    public GetDashboardQueryHandler(IContractRepository contractRepo, ITransactionRepository txRepo)
    {
        _contractRepo = contractRepo;
        _txRepo = txRepo;
    }

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken ct)
    {
        var contracts = await _contractRepo.ListAsync(null, null, ContractStatus.Active, ct);
        var today = ArgentinaTime.Today;
        var in30Days = today.AddDays(30);

        var recentTx = await _txRepo.GetRecentAsync(5, ct);
        var recentDtos = recentTx.Select(t => new TransactionDto(
            t.Id, t.ContractId, t.Type, t.Amount, t.Currency, t.Period, t.Notes, t.CreatedAt
        )).ToList();

        return new DashboardDto(
            contracts.Count,
            contracts.Sum(c => c.MonthlyRent),
            // "Expiring soon" = ends within the next 30 days. Bound below by today so already-expired
            // contracts (EndDate in the past) don't inflate the count (audit C-9).
            contracts.Count(c => c.EndDate >= today && c.EndDate <= in30Days),
            recentDtos
        );
    }
}
