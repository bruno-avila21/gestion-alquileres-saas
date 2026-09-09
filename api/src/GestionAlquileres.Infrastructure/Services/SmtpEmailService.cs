using System.Globalization;
using System.Net;
using System.Net.Mail;
using GestionAlquileres.Application.Common.Settings;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GestionAlquileres.Infrastructure.Services;

/// <summary>
/// Sends notification emails through an SMTP relay (Amazon SES, Resend, Gmail, …). Selected when
/// Email:Provider = "Smtp"; otherwise <see cref="NullEmailService"/> is used (audit M-1).
/// A send failure throws — callers that must not roll back their work (e.g. the rent-adjustment
/// handler) already wrap the call in try/catch.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private static readonly CultureInfo ArCulture = CultureInfo.GetCultureInfo("es-AR");

    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

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
        var subject = $"Ajuste de tu alquiler — {propertyAddress}";
        var body =
            $"<p>Hola {WebEncode(tenantName)},</p>" +
            $"<p>Te informamos el ajuste de tu alquiler para el inmueble <strong>{WebEncode(propertyAddress)}</strong>, " +
            $"con vigencia desde el <strong>{effectiveDate:dd/MM/yyyy}</strong>.</p>" +
            "<ul>" +
            $"<li>Alquiler anterior: <strong>{Money(previousRent)}</strong></li>" +
            $"<li>Alquiler nuevo: <strong>{Money(newRent)}</strong></li>" +
            $"<li>Índice aplicado: <strong>{adjustmentType}</strong> (coeficiente {adjustmentFactor.ToString("0.0000", ArCulture)})</li>" +
            "</ul>" +
            "<p>Ante cualquier duda, respondé este correo.</p>";

        return SendAsync(toEmail, subject, body, ct);
    }

    public Task SendContractExpiryNotificationAsync(
        string toEmail,
        string tenantName,
        string propertyAddress,
        DateOnly expiryDate,
        int daysRemaining,
        CancellationToken ct)
    {
        var subject = $"Tu contrato vence pronto — {propertyAddress}";
        var body =
            $"<p>Hola {WebEncode(tenantName)},</p>" +
            $"<p>Te recordamos que tu contrato de alquiler del inmueble <strong>{WebEncode(propertyAddress)}</strong> " +
            $"vence el <strong>{expiryDate:dd/MM/yyyy}</strong> (en {daysRemaining} días).</p>" +
            "<p>Si querés renovar o tenés consultas, escribinos respondiendo este correo.</p>";

        return SendAsync(toEmail, subject, body, ct);
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
        var subject = string.IsNullOrWhiteSpace(propertyTitle)
            ? $"Nueva consulta: {leadName}"
            : $"Nueva consulta: {leadName} — {propertyTitle}";

        var contactLines = new List<string>();
        if (!string.IsNullOrWhiteSpace(leadEmail))
            contactLines.Add($"<li>Email: <a href=\"mailto:{WebEncode(leadEmail)}\">{WebEncode(leadEmail)}</a></li>");
        if (!string.IsNullOrWhiteSpace(leadPhone))
            contactLines.Add($"<li>Teléfono: <a href=\"tel:{WebEncode(leadPhone)}\">{WebEncode(leadPhone)}</a></li>");

        var propertyLine = string.Empty;
        if (!string.IsNullOrWhiteSpace(propertyTitle) || !string.IsNullOrWhiteSpace(propertyAddress))
        {
            var propertyText = string.Join(" — ", new[] { propertyTitle, propertyAddress }
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => WebEncode(v!)));
            propertyLine = $"<p>Propiedad: <strong>{propertyText}</strong></p>";
        }

        var messageHtml = WebEncode(message).Replace(Environment.NewLine, "<br/>");

        var body =
            $"<p>Entró una nueva consulta para <strong>{WebEncode(organizationName)}</strong>.</p>" +
            $"<p>De: <strong>{WebEncode(leadName)}</strong></p>" +
            (contactLines.Count > 0 ? $"<ul>{string.Join("", contactLines)}</ul>" : string.Empty) +
            propertyLine +
            $"<p>{messageHtml}</p>" +
            $"<p style=\"color:#888;font-size:12px\">Ref: {leadId}</p>";

        return SendAsync(toEmail, subject, body, ct);
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };
        if (!string.IsNullOrWhiteSpace(_settings.Username))
            client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);

        // SmtpClient.SendMailAsync doesn't take a CancellationToken; honor cancellation up front.
        ct.ThrowIfCancellationRequested();
        await client.SendMailAsync(message);

        _logger.LogInformation("Email sent to {To} via SMTP ({Subject})", toEmail, subject);
    }

    private static string Money(decimal value) => value.ToString("C2", ArCulture);

    // Minimal HTML-escaping for the few user-controlled fields we interpolate into the body.
    private static string WebEncode(string value) => System.Net.WebUtility.HtmlEncode(value);
}
