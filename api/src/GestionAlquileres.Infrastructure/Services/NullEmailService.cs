using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace GestionAlquileres.Infrastructure.Services;

/// <summary>
/// Dev/test email service — logs notifications, does not send real emails.
/// Swap for ResendEmailService or SmtpEmailService in production.
/// </summary>
public class NullEmailService : IEmailService
{
    private readonly ILogger<NullEmailService> _logger;
    public NullEmailService(ILogger<NullEmailService> logger) => _logger = logger;

    public Task SendRentAdjustmentNotificationAsync(
        string toEmail,
        string tenantName,
        string propertyAddress,
        decimal previousRent,
        decimal newRent,
        AdjustmentType adjustmentType,
        decimal adjustmentFactor,
        DateOnly effectiveDate,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[EMAIL-STUB] Rent adjustment notification → {To} | Tenant: {Tenant} | {Property} | " +
            "{Prev:N2} → {New:N2} ({Type} ×{Factor:F4}) | Effective: {Date:yyyy-MM-dd}",
            toEmail, tenantName, propertyAddress,
            previousRent, newRent, adjustmentType, adjustmentFactor, effectiveDate);

        return Task.CompletedTask;
    }

    public Task SendContractExpiryNotificationAsync(
        string toEmail,
        string tenantName,
        string propertyAddress,
        DateOnly expiryDate,
        int daysRemaining,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[EMAIL-STUB] Contract expiry notification → {To} | Tenant: {Tenant} | {Property} | " +
            "Expires: {Date:yyyy-MM-dd} ({Days} days remaining)",
            toEmail, tenantName, propertyAddress, expiryDate, daysRemaining);

        return Task.CompletedTask;
    }

    public Task SendNewLeadNotificationAsync(
        string toEmail,
        string organizationName,
        string leadName,
        string? leadEmail,
        string? leadPhone,
        string message,
        string? propertyTitle,
        string? propertyAddress,
        Guid leadId,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[EMAIL-STUB] New lead notification → {To} | Org: {Org} | Lead: {LeadName} | " +
            "Property: {PropertyTitle} | LeadId: {LeadId}",
            toEmail, organizationName, leadName, propertyTitle, leadId);

        return Task.CompletedTask;
    }
}
