using System.Globalization;
using GestionAlquileres.Domain.Formatting;

namespace GestionAlquileres.Infrastructure.Reports;

/// <summary>
/// Formato de los PDF del negocio. La cultura la define <see cref="ArgentineCulture"/>, en Domain:
/// también se formatean importes fuera de los reportes, y dos definiciones de la misma cultura
/// terminan separándose. Acá quedan sólo los formatos propios del PDF.
/// </summary>
public static class ReportFormats
{
    public static readonly CultureInfo Argentina = ArgentineCulture.Instance;

    /// <summary>Importe con miles en punto y decimales en coma: 420000 → "420.000,00".</summary>
    public static string Money(decimal amount) => amount.ToString("N2", Argentina);

    /// <summary>Porcentaje sin ceros de más: 8 → "8", 8.5 → "8,5".</summary>
    public static string Percent(decimal value) => value.ToString("0.##", Argentina);

    public static string Date(DateOnly date) => date.ToString("dd/MM/yyyy", Argentina);

    public static string MonthYear(DateOnly date) => date.ToString("MM/yyyy", Argentina);
}
