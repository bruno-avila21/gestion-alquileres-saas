namespace GestionAlquileres.Domain.Entities;

/// <summary>
/// Paletas admitidas para el panel de administración.
///
/// Es una lista cerrada y no un color libre a propósito. El panel es una herramienta de trabajo:
/// cada paleta está calibrada para que el contraste del texto sobre el color de acción, los
/// estados de foco y los chips de estado sigan siendo legibles. Un `#RRGGBB` cualquiera elegido
/// desde un selector rompe eso en silencio — un amarillo con texto blanco encima deja de leerse
/// y nadie se entera hasta que un operador no encuentra el botón de cobrar.
///
/// El color libre sí existe, pero es <c>Organization.BrandColor</c>: el de la inmobiliaria, que
/// va impreso en los PDF, donde el riesgo es estético y no operativo.
/// </summary>
public static class PanelPalettes
{
    /// <summary>Azul institucional. La que trae el producto.</summary>
    public const string Azul = "azul";

    /// <summary>Violeta. La del sistema de diseño nuevo.</summary>
    public const string Violeta = "violeta";

    /// <summary>Carbón: neutra, muy sobria.</summary>
    public const string Carbon = "carbon";

    /// <summary>Vino: cálida.</summary>
    public const string Vino = "vino";

    public static readonly IReadOnlyList<string> All = [Azul, Violeta, Carbon, Vino];

    /// <summary>La que se usa cuando la organización no eligió ninguna.</summary>
    public const string Default = Azul;

    public static bool IsValid(string? value) =>
        value is not null && All.Contains(value);
}
