using GestionAlquileres.Application.Common.Time;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GestionAlquileres.API.Jobs;

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
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var today = ArgentinaTime.Today;
        // Raw query bypasses tenant filter — required for background jobs that process all orgs.
        var expiring = await contractRepo.GetExpiringRawAsync(NotifyDaysBefore, ct);

        foreach (var contract in expiring)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(contract.AppTenant?.Email)) continue;

                var daysRemaining = contract.EndDate.DayNumber - today.DayNumber;

                await email.SendContractExpiryNotificationAsync(
                    toEmail: contract.AppTenant.Email,
                    tenantName: $"{contract.AppTenant.FirstName} {contract.AppTenant.LastName}",
                    propertyAddress: contract.Property?.Address ?? string.Empty,
                    expiryDate: contract.EndDate,
                    daysRemaining: daysRemaining,
                    ct: ct);

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
    }
}
