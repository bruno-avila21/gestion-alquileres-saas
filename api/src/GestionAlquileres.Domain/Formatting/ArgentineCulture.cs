using System.Globalization;

namespace GestionAlquileres.Domain.Formatting;

/// <summary>
/// Cultura de formato del negocio: miles en punto, decimales en coma.
///
/// Vive en Domain y NO se usa la cultura del proceso a propósito: la imagen Linux de la API no
/// define LANG, así que .NET cae en la cultura invariante y los importes salían al revés
/// ($ 420,000.00) sólo en producción — en Windows, con es-AR, el bug era invisible. Se arma sobre
/// InvariantCulture en vez de pedir "es-AR" para no depender de que la imagen traiga datos ICU.
///
/// Está acá, y no en Infrastructure junto a los PDF, porque los importes y porcentajes también se
/// escriben desde Application (el detalle del punitorio, por ejemplo). Dos definiciones de la misma
/// cultura terminan separándose, y el bug sólo se ve en el contenedor.
/// </summary>
public static class ArgentineCulture
{
    public static readonly CultureInfo Instance = Build();

    /// <summary>Importe con miles en punto y decimales en coma: 420000 → "420.000,00".</summary>
    public static string Money(decimal amount) => amount.ToString("N2", Instance);

    /// <summary>Porcentaje sin ceros de más: 8 → "8", 0.1 → "0,1".</summary>
    public static string Percent(decimal value) => value.ToString("0.####", Instance);

    private static CultureInfo Build()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NumberGroupSeparator = ".";
        culture.NumberFormat.NumberDecimalSeparator = ",";
        culture.NumberFormat.NumberGroupSizes = new[] { 3 };
        return culture;
    }
}
