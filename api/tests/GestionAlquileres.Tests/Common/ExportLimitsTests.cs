using GestionAlquileres.Application.Common.Export;
using Xunit;

namespace GestionAlquileres.Tests.Common;

/// <summary>
/// El tope de las exportaciones era 500 y se aplicaba en silencio: una inmobiliaria con 80
/// contratos genera unos 960 cargos al año más los pagos, así que el contador exportaba para
/// conciliar el ejercicio y recibía sólo las 500 más recientes, con HTTP 200 y sin ninguna marca
/// en el archivo.
/// </summary>
public class ExportLimitsTests
{
    // Un ejercicio contable de una cartera mediana tiene que entrar entero.
    [Fact]
    public void El_tope_cubre_holgadamente_un_ejercicio_de_una_cartera_mediana()
    {
        const int contratos = 300;
        const int movimientosPorContratoAlAnio = 24; // un cargo y un pago por mes

        Assert.True(
            ExportLimits.MaxRows >= contratos * movimientosPorContratoAlAnio,
            $"El tope de {ExportLimits.MaxRows} recorta un año de {contratos} contratos.");
    }

    // Se pide una fila de más justamente para poder distinguir "justo el tope" de "hay más".
    [Fact]
    public void Se_pide_una_fila_extra_para_detectar_el_recorte()
    {
        Assert.Equal(ExportLimits.MaxRows + 1, ExportLimits.FetchSize);
    }

    [Fact]
    public void El_aviso_de_recorte_es_una_celda_csv_valida()
    {
        var notice = ExportLimits.TruncationNotice("transacciones");

        // Va entrecomillado para que la coma interna no parta la fila.
        Assert.StartsWith("\"", notice);
        Assert.EndsWith("\"", notice);
        Assert.Contains("AVISO", notice);
        Assert.Contains("transacciones", notice);
        // Sin comillas internas sin escapar, que romperían el archivo.
        Assert.Equal(2, notice.Count(ch => ch == '"'));
    }
}
