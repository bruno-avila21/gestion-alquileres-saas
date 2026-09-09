using System.Globalization;

namespace GestionAlquileres.Infrastructure.Reports;

/// <summary>
/// Formato de los PDF del negocio. Se define acá y NO se usa la cultura del proceso a propósito:
/// la imagen Linux de la API no define LANG, así que .NET cae en la cultura invariante y los
/// importes salían al revés ($ 420,000.00) sólo en producción — en Windows, con es-AR, el bug era
/// invisible. Se arma sobre InvariantCulture en vez de pedir "es-AR" para no depender de que la
/// imagen traiga datos ICU.
/// </summary>
public static class ReportFormats
{
    public static readonly CultureInfo Argentina = Build();

    /// <summary>Importe con miles en punto y decimales en coma: 420000 → "420.000,00".</summary>
    public static string Money(decimal amount) => amount.ToString("N2", Argentina);

    /// <summary>Porcentaje sin ceros de más: 8 → "8", 8.5 → "8,5".</summary>
    public static string Percent(decimal value) => value.ToString("0.##", Argentina);

    public static string Date(DateOnly date) => date.ToString("dd/MM/yyyy", Argentina);

    public static string MonthYear(DateOnly date) => date.ToString("MM/yyyy", Argentina);

    private static CultureInfo Build()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NumberGroupSeparator = ".";
        culture.NumberFormat.NumberDecimalSeparator = ",";
        culture.NumberFormat.NumberGroupSizes = new[] { 3 };
        return culture;
    }
}
