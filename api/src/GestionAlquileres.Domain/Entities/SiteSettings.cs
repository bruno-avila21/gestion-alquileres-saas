namespace GestionAlquileres.Domain.Entities;

/// <summary>
/// Cómo se ve y qué dice el sitio público de una inmobiliaria. Uno por organización.
///
/// Va en su propia entidad y no en columnas de <see cref="Organization"/> a propósito:
/// Organization se carga en cada request para resolver el tenant, y estos campos —textos
/// largos de portada incluidos— sólo los necesitan el sitio público y su editor. Meterlos
/// ahí haría más pesada la consulta más caliente del sistema para servir a la más fría.
///
/// Todo es nullable: una fila ausente, o un campo en null, significa "usá lo que trae el
/// diseño por defecto". Así una inmobiliaria que no toca nada tiene un sitio terminado, y
/// el que quiere cambiar sólo el color no tiene que redactar la portada entera.
/// </summary>
public class SiteSettings
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    /// <summary>Hex `#RRGGBB` del color de acción del sitio. Null = el violeta del diseño.</summary>
    public string? AccentColor { get; set; }

    /// <summary>Una de las combinaciones de <see cref="SiteFontPairings"/>. Null = la de por defecto.</summary>
    public string? FontPairing { get; set; }

    /// <summary>Título grande de la portada.</summary>
    public string? HeroTitle { get; set; }

    /// <summary>Bajada de la portada, debajo del título.</summary>
    public string? HeroSubtitle { get; set; }

    /// <summary>El texto de "La Empresa".</summary>
    public string? AboutText { get; set; }

    /// <summary>Frase corta del pie, debajo del nombre.</summary>
    public string? FooterTagline { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Combinaciones tipográficas admitidas para el sitio.
///
/// Es una lista cerrada y no una fuente libre: cada combinación está elegida para que el
/// título y el cuerpo funcionen juntos y para que las dos familias existan en Google Fonts
/// con los pesos que el diseño usa. Un nombre de fuente escrito a mano que no exista deja
/// el sitio en la fuente de sistema sin que nadie se entere.
/// </summary>
public static class SiteFontPairings
{
    /// <summary>Plus Jakarta Sans para todo. La del diseño aprobado.</summary>
    public const string Jakarta = "jakarta";

    /// <summary>Fraunces en los títulos, Plus Jakarta Sans en el cuerpo. Más editorial.</summary>
    public const string Editorial = "editorial";

    /// <summary>Archivo en todo. Más seca y compacta, para carteles densos.</summary>
    public const string Compacta = "compacta";

    public static readonly IReadOnlyList<string> All = [Jakarta, Editorial, Compacta];

    public const string Default = Jakarta;

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}
