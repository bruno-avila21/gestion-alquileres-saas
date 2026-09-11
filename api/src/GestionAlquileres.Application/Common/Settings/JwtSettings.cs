namespace GestionAlquileres.Application.Common.Settings;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    /// <summary>
    /// Vida del access token, en horas. Heredado: preferir <see cref="AccessTokenMinutes"/>, que da
    /// la granularidad necesaria para una ventana de revocación corta.
    /// </summary>
    public int ExpiryHours { get; set; } = 8;

    /// <summary>
    /// Vida del access token en minutos. Tiene precedencia sobre <see cref="ExpiryHours"/>.
    ///
    /// El access token es autocontenido: mientras no expira sigue valiendo aunque el usuario cierre
    /// sesión o lo den de baja. Esa duración ES la ventana de revocación del sistema, así que
    /// conviene que sea corta — la continuidad de la sesión la da el refresh token, que sí se
    /// verifica contra la base en cada canje.
    /// </summary>
    public int? AccessTokenMinutes { get; set; }

    /// <summary>Duración efectiva del access token, resolviendo la precedencia entre ambos valores.</summary>
    public int EffectiveAccessTokenMinutes =>
        AccessTokenMinutes is > 0 ? AccessTokenMinutes.Value : ExpiryHours * 60;

    /// <summary>How long an issued refresh token stays valid, in days.</summary>
    public int RefreshTokenDays { get; set; } = 14;
}
