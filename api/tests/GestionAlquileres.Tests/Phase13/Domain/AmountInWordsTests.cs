using GestionAlquileres.Domain.Reports;

namespace GestionAlquileres.Tests.Phase13.Domain;

/// <summary>
/// Bloque PDF recibos/liquidaciones, parte B. Cubre uno por uno los casos que el contrato exige
/// ("Casos que los tests tienen que cubrir sí o sí") más el rango soportado.
/// </summary>
[Trait("Phase", "Phase13")]
public class AmountInWordsTests
{
    [Fact]
    public void T00_Cero()
    {
        Assert.Equal("cero con 00/100", AmountInWords.Convert(0m));
    }

    [Fact]
    public void T01_Uno_apocopa_a_un()
    {
        Assert.Equal("un con 00/100", AmountInWords.Convert(1m));
    }

    [Fact]
    public void T02_Quince()
    {
        Assert.Equal("quince con 00/100", AmountInWords.Convert(15m));
    }

    [Fact]
    public void T03_Dieciseis()
    {
        Assert.Equal("dieciséis con 00/100", AmountInWords.Convert(16m));
    }

    [Fact]
    public void T04_VeintiunoApocopaAVeintiun()
    {
        Assert.Equal("veintiún con 00/100", AmountInWords.Convert(21m));
    }

    [Fact]
    public void T05_Treinta()
    {
        Assert.Equal("treinta con 00/100", AmountInWords.Convert(30m));
    }

    [Fact]
    public void T06_Cien_sin_ceinto()
    {
        Assert.Equal("cien con 00/100", AmountInWords.Convert(100m));
    }

    [Fact]
    public void T07_CientoUno_no_apocopa()
    {
        // El contrato pide explícitamente "ciento uno", no "ciento un".
        Assert.Equal("ciento uno con 00/100", AmountInWords.Convert(101m));
    }

    [Fact]
    public void T08_Doscientos()
    {
        Assert.Equal("doscientos con 00/100", AmountInWords.Convert(200m));
    }

    [Fact]
    public void T09_Quinientos()
    {
        Assert.Equal("quinientos con 00/100", AmountInWords.Convert(500m));
    }

    [Fact]
    public void T10_Novecientos()
    {
        Assert.Equal("novecientos con 00/100", AmountInWords.Convert(900m));
    }

    [Fact]
    public void T11_Mil_sin_un()
    {
        Assert.Equal("mil con 00/100", AmountInWords.Convert(1000m));
    }

    [Fact]
    public void T12_MilUno()
    {
        Assert.Equal("mil un con 00/100", AmountInWords.Convert(1001m));
    }

    [Fact]
    public void T13_VeintiunMil()
    {
        Assert.Equal("veintiún mil con 00/100", AmountInWords.Convert(21000m));
    }

    [Fact]
    public void T14_CienMil()
    {
        Assert.Equal("cien mil con 00/100", AmountInWords.Convert(100000m));
    }

    [Fact]
    public void T15_UnMillon()
    {
        Assert.Equal("un millón con 00/100", AmountInWords.Convert(1000000m));
    }

    [Fact]
    public void T16_DosMillones()
    {
        Assert.Equal("dos millones con 00/100", AmountInWords.Convert(2000000m));
    }

    [Fact]
    public void T17_TopeDelRango()
    {
        Assert.Equal(
            "novecientos noventa y nueve millones novecientos noventa y nueve mil novecientos noventa y nueve con 99/100",
            AmountInWords.Convert(999999999.99m));
    }

    [Fact]
    public void T18_CincoCentavos()
    {
        Assert.Equal("cero con 05/100", AmountInWords.Convert(0.05m));
    }

    [Fact]
    public void T19_MilDoscientosTreintaYCuatroConCincuenta()
    {
        Assert.Equal("mil doscientos treinta y cuatro con 50/100", AmountInWords.Convert(1234.5m));
    }

    [Fact]
    public void T20_FueraDeRango_Negativo_lanza()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AmountInWords.Convert(-0.01m));
    }

    [Fact]
    public void T21_FueraDeRango_PorArriba_lanza()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AmountInWords.Convert(1_000_000_000m));
    }

    [Fact]
    public void T22_LimiteSuperiorExacto_noLanza()
    {
        Assert.NotEmpty(AmountInWords.Convert(999_999_999.99m));
    }
}
