using GestionAlquileres.Domain.Enums;
using Xunit;

namespace GestionAlquileres.Tests.Phase9;

/// <summary>
/// La cadencia de ajuste se ancla al inicio del contrato: <c>StartDate + k·frecuencia</c>.
///
/// El scheduler encadenaba desde la fecha efectiva del último ajuste, y eso desplazaba el día en
/// contratos que arrancan el 29, 30 o 31: <c>AddMonths</c> recorta al último día del mes destino,
/// así que 31-ene + 1 mes = 28-feb, y la corrida siguiente tomaba ESE 28 como base dando 28-mar.
/// La cadencia se corría y no volvía nunca al 31.
///
/// Estos tests fijan la aritmética de fechas que usa <c>MonthlyRentAdjustmentJob.ProcessOneAsync</c>.
/// </summary>
public class AdjustmentCadenceTests
{
    /// <summary>Réplica exacta del cálculo del job: siempre desde StartDate, nunca encadenando.</summary>
    private static DateOnly NextAdjustment(DateOnly startDate, AdjustmentFrequency frequency, int applied) =>
        startDate.AddMonths((applied + 1) * frequency.ToMonths());

    // El caso que motivó el arreglo: contrato que arranca un 31, ajuste mensual.
    [Fact]
    public void Un_contrato_que_arranca_el_31_no_pierde_el_dia()
    {
        var start = new DateOnly(2026, 1, 31);

        Assert.Equal(new DateOnly(2026, 2, 28), NextAdjustment(start, AdjustmentFrequency.Monthly, 0));
        // Acá estaba el bug: encadenando desde el 28-feb daba 28-mar. Anclando al inicio, vuelve al 31.
        Assert.Equal(new DateOnly(2026, 3, 31), NextAdjustment(start, AdjustmentFrequency.Monthly, 1));
        Assert.Equal(new DateOnly(2026, 4, 30), NextAdjustment(start, AdjustmentFrequency.Monthly, 2));
        Assert.Equal(new DateOnly(2026, 5, 31), NextAdjustment(start, AdjustmentFrequency.Monthly, 3));
    }

    [Fact]
    public void Un_contrato_que_arranca_el_30_tampoco_deriva()
    {
        var start = new DateOnly(2026, 1, 30);

        Assert.Equal(new DateOnly(2026, 2, 28), NextAdjustment(start, AdjustmentFrequency.Monthly, 0));
        Assert.Equal(new DateOnly(2026, 3, 30), NextAdjustment(start, AdjustmentFrequency.Monthly, 1));
    }

    // Febrero de un año bisiesto: el recorte es a 29, y el mes siguiente vuelve al día original.
    [Fact]
    public void El_anio_bisiesto_no_rompe_la_cadencia()
    {
        var start = new DateOnly(2028, 1, 31);

        Assert.Equal(new DateOnly(2028, 2, 29), NextAdjustment(start, AdjustmentFrequency.Monthly, 0));
        Assert.Equal(new DateOnly(2028, 3, 31), NextAdjustment(start, AdjustmentFrequency.Monthly, 1));
    }

    [Theory]
    [InlineData(AdjustmentFrequency.Quarterly, 3)]
    [InlineData(AdjustmentFrequency.FourMonthly, 4)]
    [InlineData(AdjustmentFrequency.SemiAnnual, 6)]
    [InlineData(AdjustmentFrequency.Annual, 12)]
    public void Las_frecuencias_avanzan_el_periodo_completo(AdjustmentFrequency frequency, int months)
    {
        var start = new DateOnly(2026, 1, 15);

        Assert.Equal(start.AddMonths(months), NextAdjustment(start, frequency, 0));
        Assert.Equal(start.AddMonths(months * 2), NextAdjustment(start, frequency, 1));
    }

    // Documenta la propiedad que el arreglo garantiza: el día del mes se conserva salvo cuando el
    // mes destino no lo tiene, y en ese caso NO se propaga al período siguiente.
    [Fact]
    public void El_dia_se_recupera_despues_de_un_mes_corto()
    {
        var start = new DateOnly(2026, 8, 31);
        var dias = Enumerable.Range(0, 6)
            .Select(k => NextAdjustment(start, AdjustmentFrequency.Monthly, k).Day)
            .ToArray();

        // sep(30) oct(31) nov(30) dic(31) ene(31) feb(28)
        Assert.Equal(new[] { 30, 31, 30, 31, 31, 28 }, dias);
    }
}
