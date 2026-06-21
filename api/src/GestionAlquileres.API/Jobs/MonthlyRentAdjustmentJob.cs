using GestionAlquileres.Application.Common.Time;
using GestionAlquileres.Application.Features.RentHistory.Commands;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GestionAlquileres.API.Jobs;

public class MonthlyRentAdjustmentJob
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<MonthlyRentAdjustmentJob> _logger;

    public MonthlyRentAdjustmentJob(IServiceProvider sp, ILogger<MonthlyRentAdjustmentJob> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        await using var scope = _sp.CreateAsyncScope();
        var contractRepo = scope.ServiceProvider.GetRequiredService<IContractRepository>();
        var historyRepo = scope.ServiceProvider.GetRequiredService<IRentHistoryRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var today = ArgentinaTime.Today;
        // Raw query bypasses tenant filter — required for background jobs that process all orgs.
        var contracts = await contractRepo.ListActiveRawAsync(ct);

        foreach (var contract in contracts)
        {
            try
            {
                if (contract.AdjustmentType == AdjustmentType.Manual) continue;

                int frequencyMonths = contract.AdjustmentFrequency switch
                {
                    AdjustmentFrequency.Monthly => 1,
                    AdjustmentFrequency.Quarterly => 3,
                    AdjustmentFrequency.Annual => 12,
                    _ => 3,
                };

                var lastAdj = await historyRepo.GetLastByContractRawAsync(contract.Id, ct);
                var baseDate = lastAdj?.EffectiveDate ?? contract.StartDate;
                var nextAdj = baseDate.AddMonths(frequencyMonths);

                if (today < nextAdj) continue;

                // Anchor the adjustment to the *scheduled* date, not the day the job happens to run.
                // Passing `today` would persist EffectiveDate=today, so next period's base becomes
                // today and the whole schedule drifts forward permanently whenever the job runs late;
                // it would also pick the index period of the run month instead of the scheduled
                // month. Using `nextAdj` keeps the cadence pinned to StartDate + k·frequency and
                // self-heals one overdue period per run (audit C-3).
                await mediator.Send(
                    new ApplyRentAdjustmentCommand(contract.Id, nextAdj, OrganizationId: contract.OrganizationId), ct);
                _logger.LogInformation(
                    "Monthly adjustment applied to contract {ContractId} for scheduled date {Date} (run {Today})",
                    contract.Id, nextAdj, today);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to apply monthly adjustment to contract {ContractId}",
                    contract.Id);
            }
        }
    }
}
