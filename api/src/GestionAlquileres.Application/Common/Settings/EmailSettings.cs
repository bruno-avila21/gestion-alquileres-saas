namespace GestionAlquileres.Application.Common.Settings;

/// <summary>
/// Email delivery configuration. Provider "Null" only logs (dev/test default); "Smtp" sends real
/// mail through any SMTP relay (Amazon SES, Resend, Gmail, etc.). Credentials must come from
/// environment/user-secrets, never appsettings in the repo.
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Null"; // "Null" | "Smtp"

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;

    public string? Username { get; set; }
    public string? Password { get; set; }

    public string FromEmail { get; set; } = "no-reply@gestionalquileres.app";
    public string FromName { get; set; } = "Gestión de Alquileres";
}
