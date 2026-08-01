using GestionAlquileres.Application.Common.Time;
using GestionAlquileres.Application.Features.RentHistory.Commands;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Hangfire;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GestionAlquileres.API.Jobs;

// Prevent overlapping runs (a slow run + the next tick) from double-processing contracts. The
// per-effective-date unique index is the last-resort guard, but not stacking the work is cheaper
// and avoids needless 409s (audit M5).
[DisableConcurrentExecution(timeoutInSeconds: 30 * 60)]
public class MonthlyRentAdjustmentJob
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<MonthlyRentAdjustmentJob> _logger;

    public MonthlyRentAdjustmentJob(IServiceProvider sp, ILogger<MonthlyRentAdjustmentJob> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    /// <summary>Minimal snapshot of a contract, detached from the fetch scope's DbContext.</summary>
    private readonly record struct ContractRef(
        Guid Id, Guid OrganizationId, AdjustmentType AdjustmentType,
        AdjustmentFrequency Frequency, DateOnly StartDate);

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var today = ArgentinaTime.Today;

        // Fetch the work list once in its own short-lived scope, then let that DbContext go. Each
        // contract is then processed in its OWN scope/DbContext below, so the change tracker never
        // accumulates across contracts and a single failure can't poison a shared unit of work
        // (audit A3). This keeps memory flat regardless of the number of active contracts.
        List<ContractRef> work;
        await using (var scope = _sp.CreateAsyncScope())
        {
            var contractRepo = scope.ServiceProvider.GetRequiredService<IContractRepository>();
            // Raw query bypasses the tenant filter — required for a background job that spans all orgs.
            var contracts = await contractRepo.ListActiveRawAsync(ct);
            work = contracts
                .Where(c => c.AdjustmentType != AdjustmentType.Manual)
                .Select(c => new ContractRef(c.Id, c.OrganizationId, c.AdjustmentType, c.AdjustmentFrequency, c.StartDate))
                .ToList();
        }

        var applied = 0;
        foreach (var contract in work)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                if (await ProcessOneAsync(contract, today, ct)) applied++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to apply monthly adjustment to contract {ContractId}", contract.Id);
            }
        }

        _logger.LogInformation(
            "Monthly adjustment run complete: {Applied}/{Total} contracts adjusted (run {Today})",
            applied, work.Count, today);
    }

    private async Task<bool> ProcessOneAsync(ContractRef contract, DateOnly today, CancellationToken ct)
    {
        await using var scope = _sp.CreateAsyncScope();
        var historyRepo = scope.ServiceProvider.GetRequiredService<IRentHistoryRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        int frequencyMonths = contract.Frequency.ToMonths();

        var lastAdj = await historyRepo.GetLastByContractRawAsync(contract.Id, ct);
        var baseDate = lastAdj?.EffectiveDate ?? contract.StartDate;
        var nextAdj = baseDate.AddMonths(frequencyMonths);

        if (today < nextAdj) return false;

        // Anchor the adjustment to the *scheduled* date, not the day the job happens to run, so the
        // cadence stays pinned to StartDate + k·frequency and self-heals one overdue period per run
        // (audit C-3).
        await mediator.Send(
            new ApplyRentAdjustmentCommand(contract.Id, nextAdj, OrganizationId: contract.OrganizationId), ct);
        _logger.LogInformation(
            "Monthly adjustment applied to contract {ContractId} for scheduled date {Date} (run {Today})",
            contract.Id, nextAdj, today);
        return true;
    }
}
