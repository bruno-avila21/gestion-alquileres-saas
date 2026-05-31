namespace GestionAlquileres.API.Configuration;

/// <summary>
/// Fail-fast validation of security-sensitive configuration at startup.
///
/// Goal: never let the application run with a known/placeholder secret or a hard-coded
/// dev password — especially in production. Secrets must be supplied via environment
/// variables, user-secrets, or a (git-ignored) appsettings.{Environment}.json.
/// </summary>
public static class SecuritySettingsValidator
{
    private const int MinSecretLength = 32;
    private const string PlaceholderMarker = "REPLACE_WITH";
    private const string ForbiddenDbPassword = "devpassword";

    public static void Validate(IConfiguration config, IHostEnvironment env)
    {
        var problems = new List<string>();
        var isDev = env.IsDevelopment();

        ValidateSecret(config["JwtSettings:SecretKey"], "JwtSettings:SecretKey", isDev, problems);
        ValidateSecret(config["DocumentToken:Secret"], "DocumentToken:Secret", isDev, problems);

        ValidateConnectionString(config.GetConnectionString("DefaultConnection"), "ConnectionStrings:DefaultConnection", isDev, problems);
        ValidateConnectionString(config.GetConnectionString("HangfireConnection"), "ConnectionStrings:HangfireConnection", isDev, problems);

        if (problems.Count == 0) return;

        var message = "Configuración de seguridad inválida:" + Environment.NewLine +
                      string.Join(Environment.NewLine, problems.Select(p => "  - " + p));

        // Example placeholders ("REPLACE_WITH…") are never acceptable, in any environment.
        // Missing/weak secrets and the local dev password are a hard error outside Development;
        // inside Development they are only a warning so that local runs and the test host
        // (which inject their own secrets) are not blocked — the local DB legitimately uses devpassword.
        var fatal = problems.Any(p => p.Contains(PlaceholderMarker, StringComparison.OrdinalIgnoreCase))
                    || !isDev;

        if (fatal)
            throw new InvalidOperationException(message +
                Environment.NewLine + "Configure los secretos vía variables de entorno o appsettings.{Environment}.json.");

        Console.Error.WriteLine("[ADVERTENCIA] " + message);
    }

    private static void ValidateSecret(string? value, string key, bool isDev, List<string> problems)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Contains(PlaceholderMarker, StringComparison.OrdinalIgnoreCase))
        {
            problems.Add($"{key} contiene el placeholder '{PlaceholderMarker}'. No use valores de ejemplo.");
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
            problems.Add($"{key} no está configurado.");
        else if (value.Length < MinSecretLength)
            problems.Add($"{key} debe tener al menos {MinSecretLength} caracteres (actual: {value.Length}).");
    }

    private static void ValidateConnectionString(string? value, string key, bool isDev, List<string> problems)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Contains(ForbiddenDbPassword, StringComparison.OrdinalIgnoreCase))
        {
            problems.Add($"{key} contiene la contraseña de desarrollo '{ForbiddenDbPassword}'. Use una credencial segura por entorno.");
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
            problems.Add($"{key} no está configurado.");
    }
}
