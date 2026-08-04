using GestionAlquileres.Application.Common.Time;
using GestionAlquileres.Application.Features.Transactions.Commands;
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
        var today = ArgentinaTime.Today;
        var in30Days = today.AddDays(30);

        // Los agregados se calculan en la base. Antes se traía la cartera activa completa, con
        // Property y AppTenant incluidos, para contar y sumar en memoria.
        var (activeCount, monthlyRevenue, expiringCount) =
            await _contractRepo.GetDashboardStatsAsync(today, in30Days, ct);

        var recentTx = await _txRepo.GetRecentAsync(5, ct);
        var recentDtos = recentTx.Select(RegisterPaymentCommandHandler.ToDto).ToList();

        return new DashboardDto(activeCount, monthlyRevenue, expiringCount, recentDtos);
    }
}
