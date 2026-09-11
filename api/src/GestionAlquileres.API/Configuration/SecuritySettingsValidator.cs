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

        ValidateStorageProvider(config["Storage:Provider"], isDev, problems);
        ValidateRegistrationMode(config["Registration:Mode"], config["Registration:InviteCode"], isDev, problems);
        ValidateAllowedHosts(config["AllowedHosts"], isDev, problems);

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

    private static void ValidateStorageProvider(string? provider, bool isDev, List<string> problems)
    {
        // Local FS storage is a single point of failure: it lives on one node's disk, isn't shared
        // across instances and is lost on a container restart. Acceptable for development only — any
        // other environment must use the S3-compatible object store (audit A-3).
        if (isDev) return;

        if (string.IsNullOrWhiteSpace(provider) || !provider.Equals("S3", StringComparison.OrdinalIgnoreCase))
            problems.Add("Storage:Provider debe ser 'S3' fuera de Development. El almacenamiento local es un punto único de fallo y no se comparte entre instancias.");
    }

    private static void ValidateRegistrationMode(
        string? mode, string? inviteCode, bool isDev, List<string> problems)
    {
        // El alta de organizaciones es un endpoint anónimo: en modo abierto cualquiera crea
        // organizaciones ilimitadas con emails que no controla, obtiene un JWT de Admin al instante,
        // y ocupa slugs de marcas reales de forma irrecuperable. Aceptable sólo en desarrollo, hasta
        // que exista verificación por email.
        if (isDev) return;

        if (string.IsNullOrWhiteSpace(mode) || mode.Equals("Open", StringComparison.OrdinalIgnoreCase))
        {
            problems.Add(
                "Registration:Mode debe ser 'InviteCode' o 'Disabled' fuera de Development. " +
                "El alta abierta permite crear organizaciones sin verificar el email y ocupar slugs de forma irrecuperable.");
            return;
        }

        if (mode.Equals("InviteCode", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(inviteCode))
        {
            problems.Add("Registration:InviteCode no está configurado, pero Registration:Mode es 'InviteCode'.");
        }
    }

    private static void ValidateAllowedHosts(string? allowedHosts, bool isDev, List<string> problems)
    {
        // Con AllowedHosts en "*" el filtrado de host queda desactivado y la API refleja cualquier
        // encabezado Host que le manden. La URL absoluta de descarga de documentos se arma con ese
        // valor, así que con un CDN delante una respuesta envenenada puede servirse a otros usuarios
        // con el token adentro.
        if (isDev) return;

        if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Trim() == "*")
            problems.Add(
                "AllowedHosts debe listar los dominios reales fuera de Development. " +
                "Con '*' la API refleja cualquier encabezado Host, y la URL de descarga de documentos se arma con él.");
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
