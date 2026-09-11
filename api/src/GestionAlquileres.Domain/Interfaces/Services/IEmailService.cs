using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Domain.Interfaces.Services;

public interface IEmailService
{
    Task SendRentAdjustmentNotificationAsync(
        string toEmail,
        string tenantName,
        string propertyAddress,
        decimal previousRent,
        decimal newRent,
        AdjustmentType adjustmentType,
        decimal adjustmentFactor,
        DateOnly effectiveDate,
        CancellationToken ct);

    Task SendContractExpiryNotificationAsync(
        string toEmail,
        string tenantName,
        string propertyAddress,
        DateOnly expiryDate,
        int daysRemaining,
        CancellationToken ct);

    Task SendNewLeadNotificationAsync(
        string toEmail,
        string organizationName,
        string leadName,
        string? leadEmail,
        string? leadPhone,
        string message,
        string? propertyTitle,
        string? propertyAddress,
        Guid leadId,
        CancellationToken ct);
}
