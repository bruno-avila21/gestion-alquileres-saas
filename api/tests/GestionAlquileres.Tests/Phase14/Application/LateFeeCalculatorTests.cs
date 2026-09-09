using GestionAlquileres.Domain.Billing;

namespace GestionAlquileres.Tests.Phase14.Application;

/// <summary>
/// Bloque punitorios, parte A: la regla de plata, aislada del job y de la base.
/// </summary>
[Trait("Phase", "Phase14")]
public class LateFeeCalculatorTests
{
    private static readonly DateOnly Vencimiento = new(2026, 8, 1);

    [Theory]
    // El día del vencimiento todavía no hay mora: se debe pagar hasta ese día inclusive.
    [InlineData("2026-08-01", 0, 0)]
    [InlineData("2026-08-02", 0, 1)]
    [InlineData("2026-08-31", 0, 30)]
    // Con tolerancia, el punitorio recién corre pasados los días de gracia.
    [InlineData("2026-08-06", 5, 0)]
    [InlineData("2026-08-07", 5, 1)]
    // Antes del vencimiento no hay mora, y el resultado nunca es negativo.
    [InlineData("2026-07-20", 0, 0)]
    public void Cuenta_los_dias_de_mora_desde_el_fin_de_la_tolerancia(string asOf, int gracia, int esperado)
    {
        var dias = LateFeeCalculator.DaysInArrears(Vencimiento, gracia, DateOnly.Parse(asOf));
        Assert.Equal(esperado, dias);
    }

    [Fact]
    public void Devenga_capital_por_tasa_diaria_por_dias()
    {
        // 420.000 al 0,1% diario durante 25 días = 10.500, el caso de la pantalla.
        Assert.Equal(10_500m, LateFeeCalculator.Accrue(420_000m, 0.1m, 25));
    }

    [Fact]
    public void Redondea_a_centavos()
    {
        // 1000 × 0,0333% × 1 día = 0,333 → 0,33
        Assert.Equal(0.33m, LateFeeCalculator.Accrue(1_000m, 0.0333m, 1));
    }

    [Theory]
    [InlineData(0, 0.1, 10)]     // sin capital no hay punitorio
    [InlineData(1000, 0, 10)]    // contrato sin punitorio pactado
    [InlineData(1000, 0.1, 0)]   // todavía no hay mora
    [InlineData(1000, -1, 10)]   // una tasa negativa no puede volverse un crédito a favor
    public void No_devenga_nada_cuando_falta_capital_tasa_o_mora(decimal capital, decimal tasa, int dias)
    {
        Assert.Equal(0m, LateFeeCalculator.Accrue(capital, tasa, dias));
    }

}
