using GestionAlquileres.Domain.Enums;
using Xunit;

namespace GestionAlquileres.Tests.Common;

/// <summary>
/// El mapeo frecuencia → meses estaba duplicado en el handler del ajuste, en la proyección y en el
/// job mensual, y ya había divergido. Ahora vive en un solo lugar; estos tests lo fijan.
/// </summary>
public class AdjustmentFrequencyExtensionsTests
{
    [Theory]
    [InlineData(AdjustmentFrequency.Monthly, 1)]
    [InlineData(AdjustmentFrequency.Quarterly, 3)]
    [InlineData(AdjustmentFrequency.FourMonthly, 4)]
    [InlineData(AdjustmentFrequency.SemiAnnual, 6)]
    [InlineData(AdjustmentFrequency.Annual, 12)]
    public void ToMonths_devuelve_el_periodo_esperado(AdjustmentFrequency frequency, int expected)
    {
        Assert.Equal(expected, frequency.ToMonths());
    }

    // El `_ => 3` anterior convertía silenciosamente un valor inválido en un ajuste trimestral que
    // nadie había pactado. Preferimos que explote.
    [Fact]
    public void ToMonths_lanza_ante_un_valor_desconocido()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((AdjustmentFrequency)99).ToMonths());
    }

    // Los valores se persisten como enteros: si alguien reordena el enum, los contratos ya
    // cargados cambian de frecuencia en silencio.
    [Theory]
    [InlineData(AdjustmentFrequency.Monthly, 0)]
    [InlineData(AdjustmentFrequency.Quarterly, 1)]
    [InlineData(AdjustmentFrequency.Annual, 2)]
    [InlineData(AdjustmentFrequency.FourMonthly, 3)]
    [InlineData(AdjustmentFrequency.SemiAnnual, 4)]
    public void Los_ordinales_no_deben_cambiar(AdjustmentFrequency frequency, int ordinal)
    {
        Assert.Equal(ordinal, (int)frequency);
    }

    [Theory]
    [InlineData(AdjustmentType.ICL, 0)]
    [InlineData(AdjustmentType.IPC, 1)]
    [InlineData(AdjustmentType.Manual, 2)]
    [InlineData(AdjustmentType.FixedPercent, 3)]
    public void Los_ordinales_del_tipo_de_ajuste_no_deben_cambiar(AdjustmentType type, int ordinal)
    {
        Assert.Equal(ordinal, (int)type);
    }
}
