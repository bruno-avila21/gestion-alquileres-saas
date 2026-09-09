using System.Globalization;
using GestionAlquileres.Infrastructure.Reports;
using Xunit;

namespace GestionAlquileres.Tests.Phase13.Infrastructure;

/// <summary>
/// Regresión: los importes del PDF salían en formato estadounidense ($ 420,000.00) en el contenedor
/// Linux, porque la imagen no define LANG y .NET cae en la cultura invariante. En Windows, con
/// es-AR, el bug era invisible. El formato tiene que ser argentino sin importar la cultura del
/// proceso, así que se prueba bajo las tres culturas que importan.
/// </summary>
public class PdfCultureTests
{
    public static TheoryData<string> Cultures() => new() { "en-US", "", "es-AR" };

    [Theory]
    [MemberData(nameof(Cultures))]
    public void Money_uses_argentine_separators_regardless_of_ambient_culture(string cultureName)
    {
        UnderCulture(cultureName, () =>
        {
            Assert.Equal("420.000,00", ReportFormats.Money(420000m));
            Assert.Equal("1.234.567,89", ReportFormats.Money(1234567.89m));
            Assert.Equal("0,50", ReportFormats.Money(0.5m));
        });
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void Percent_and_dates_are_stable_regardless_of_ambient_culture(string cultureName)
    {
        UnderCulture(cultureName, () =>
        {
            Assert.Equal("8", ReportFormats.Percent(8m));
            Assert.Equal("8,5", ReportFormats.Percent(8.5m));
            Assert.Equal("09/09/2026", ReportFormats.Date(new DateOnly(2026, 9, 9)));
            Assert.Equal("09/2026", ReportFormats.MonthYear(new DateOnly(2026, 9, 9)));
        });
    }

    private static void UnderCulture(string cultureName, Action assertions)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = cultureName.Length == 0
                ? CultureInfo.InvariantCulture
                : CultureInfo.GetCultureInfo(cultureName);
            assertions();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
