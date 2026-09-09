using GestionAlquileres.Application.Common.Billing;
using GestionAlquileres.Application.Common.Time;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GestionAlquileres.API.Jobs;

/// <summary>
/// Devengamiento diario del punitorio por mora. Recalcula, para cada cargo vencido e impago de un
/// contrato con punitorio pactado, el importe acumulado a hoy.
///
/// Es idempotente por diseño: el punitorio guarda hasta qué día está devengado, así que dos
/// corridas del mismo día no suman nada. El índice único sobre related_transaction_id es la última
/// red, pero además no se apilan corridas.
/// </summary>
[DisableConcurrentExecution(timeoutInSeconds: 30 * 60)]
public class LateFeeAccrualJob
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<LateFeeAccrualJob> _logger;

    public LateFeeAccrualJob(IServiceProvider sp, ILogger<LateFeeAccrualJob> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var today = ArgentinaTime.Today;

        // Igual que el job de ajustes: la lista de trabajo se arma en un scope corto que después se
        // suelta, y cada cargo se procesa en SU propio scope/DbContext. Así el change tracker no
        // acumula a lo largo de la corrida y el fallo de un cargo no envenena una unidad de trabajo
        // compartida.
        IReadOnlyList<OverdueChargeRow> work;
        await using (var scope = _sp.CreateAsyncScope())
        {
            var txRepo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
            work = await txRepo.GetOverdueChargesForLateFeeRawAsync(today, ct);
        }

        var accrued = 0;
        foreach (var row in work)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                if (await ProcessOneAsync(row, today, ct)) accrued++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to accrue late fee for charge {ChargeId} (contract {ContractId})",
                    row.ChargeId, row.ContractId);
            }
        }

        _logger.LogInformation(
            "Late fee accrual complete: {Accrued}/{Total} overdue charges accrued (run {Today})",
            accrued, work.Count, today);
    }

    private async Task<bool> ProcessOneAsync(OverdueChargeRow row, DateOnly today, CancellationToken ct)
    {
        await using var scope = _sp.CreateAsyncScope();
        var txRepo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var lateFees = scope.ServiceProvider.GetRequiredService<ILateFeeAccrualService>();

        // Se relee dentro del scope propio: la fila del fetch es una proyección desconectada, y para
        // escribir hace falta la entidad trackeada por ESTE DbContext.
        var charge = await txRepo.GetByIdRawAsync(row.ChargeId, row.OrganizationId, ct);
        if (charge is null) return false;

        var lateFee = await lateFees.AccrueAsync(charge, row.DailyRate, row.GraceDays, today, ct);
        if (lateFee is null) return false;

        await txRepo.SaveChangesAsync(ct);
        return true;
    }
}
