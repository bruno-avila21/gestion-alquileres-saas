namespace GestionAlquileres.Application.Common.Settings;

public enum RegistrationMode
{
    /// <summary>Cualquiera puede crear una organización. Sólo apto para desarrollo.</summary>
    Open = 0,

    /// <summary>El alta exige un código que se comparte fuera de banda con quien se da de alta.</summary>
    InviteCode = 1,

    /// <summary>El alta por API está cerrada; las organizaciones se crean por otra vía.</summary>
    Disabled = 2,
}

/// <summary>
/// Control del alta de organizaciones.
///
/// El endpoint era anónimo y sin ninguna verificación: cualquiera creaba organizaciones ilimitadas
/// con emails que no controla y obtenía un JWT de Admin al instante. Además permitía ocupar slugs
/// de marcas reales de forma IRRECUPERABLE, porque el alta los bloquea para siempre.
///
/// La solución de fondo es verificar el email, que depende de tener SMTP funcionando. Mientras
/// tanto el alta pasa a ser controlable, y <c>SecuritySettingsValidator</c> impide arrancar en modo
/// abierto fuera de Development.
/// </summary>
public class RegistrationSettings
{
    public const string SectionName = "Registration";

    public RegistrationMode Mode { get; set; } = RegistrationMode.Open;

    /// <summary>Código requerido cuando <see cref="Mode"/> es <see cref="RegistrationMode.InviteCode"/>.</summary>
    public string? InviteCode { get; set; }
}
