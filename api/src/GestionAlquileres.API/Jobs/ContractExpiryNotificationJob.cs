using GestionAlquileres.Application.Common.Time;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GestionAlquileres.API.Jobs;

/// <summary>
/// Avisa al inquilino que su contrato está por vencer.
///
/// El job corre a diario sobre una ventana de 30 días, así que SIN deduplicación el mismo inquilino
/// recibía el mismo aviso hasta 30 veces seguidas. Cada envío queda registrado en
/// <see cref="SentNotification"/>, con la fecha de fin del contrato como clave: si el contrato se
/// renueva con una fecha nueva se vuelve a avisar, y mientras no cambie se avisa una sola vez.
/// </summary>
[DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
public class ContractExpiryNotificationJob
{
    private const int NotifyDaysBefore = 30;

    private readonly IServiceProvider _sp;
    private readonly ILogger<ContractExpiryNotificationJob> _logger;

    public ContractExpiryNotificationJob(IServiceProvider sp, ILogger<ContractExpiryNotificationJob> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        await using var scope = _sp.CreateAsyncScope();
        var contractRepo = scope.ServiceProvider.GetRequiredService<IContractRepository>();
        var sentRepo = scope.ServiceProvider.GetRequiredService<ISentNotificationRepository>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var today = ArgentinaTime.Today;
        // Raw query bypasses tenant filter — required for background jobs that process all orgs.
        var expiring = await contractRepo.GetExpiringRawAsync(NotifyDaysBefore, ct);

        var sent = 0;
        var skipped = 0;

        foreach (var contract in expiring)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(contract.AppTenant?.Email)) continue;

                var dedupeKey = SentNotification.ExpiryKey(contract.EndDate);

                if (await sentRepo.ExistsRawAsync(
                        contract.Id, contract.OrganizationId, NotificationKind.ContractExpiry, dedupeKey, ct))
                {
                    skipped++;
                    continue;
                }

                var daysRemaining = contract.EndDate.DayNumber - today.DayNumber;

                await email.SendContractExpiryNotificationAsync(
                    toEmail: contract.AppTenant.Email,
                    tenantName: $"{contract.AppTenant.FirstName} {contract.AppTenant.LastName}",
                    propertyAddress: contract.Property?.Address ?? string.Empty,
                    expiryDate: contract.EndDate,
                    daysRemaining: daysRemaining,
                    ct: ct);

                // Se registra DESPUÉS del envío: si el relay falla, el aviso se reintenta mañana en
                // vez de darse por enviado. El índice único (ContractId, Kind, DedupeKey) cubre el
                // caso raro de una caída entre el envío y el commit.
                await sentRepo.AddAsync(new SentNotification
                {
                    OrganizationId = contract.OrganizationId,
                    ContractId = contract.Id,
                    Kind = NotificationKind.ContractExpiry,
                    DedupeKey = dedupeKey,
                }, ct);
                await sentRepo.SaveChangesAsync(ct);

                sent++;
                _logger.LogInformation(
                    "Expiry notification sent for contract {ContractId} expiring on {Date} ({Days} days)",
                    contract.Id, contract.EndDate, daysRemaining);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send expiry notification for contract {ContractId}",
                    contract.Id);
            }
        }

        _logger.LogInformation(
            "Contract expiry job finished: {Sent} sent, {Skipped} already notified, {Total} in window",
            sent, skipped, expiring.Count);
    }
}
