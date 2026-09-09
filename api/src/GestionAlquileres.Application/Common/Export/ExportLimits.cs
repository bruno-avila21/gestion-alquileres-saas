namespace GestionAlquileres.Application.Common.Export;

/// <summary>
/// Tope de filas de las exportaciones a CSV.
///
/// Antes era 500 y se aplicaba en silencio: una inmobiliaria con 80 contratos genera unos 960
/// cargos de alquiler al año más los pagos, así que el contador exportaba para conciliar el
/// ejercicio y recibía sólo las 500 más recientes, con HTTP 200 y sin ninguna marca en el archivo.
/// El error era indetectable desde el CSV.
///
/// El tope sigue existiendo —una exportación sin techo es una vía fácil de agotar la memoria del
/// proceso— pero ahora es lo bastante alto para no recortar datos reales, y cuando se alcanza el
/// archivo lo declara.
/// </summary>
public static class ExportLimits
{
    public const int MaxRows = 50_000;

    /// <summary>
    /// Se piden MaxRows + 1 filas: si vuelven todas, sabemos que había al menos una más y el
    /// resultado está recortado.
    /// </summary>
    public const int FetchSize = MaxRows + 1;

    public const string TruncatedHeader = "X-Export-Truncated";

    /// <summary>
    /// Fila final de aviso. Va dentro del CSV y no sólo en un header porque quien lo abre lo hace
    /// en una planilla, donde los headers HTTP no se ven.
    /// </summary>
    public static string TruncationNotice(string what) =>
        $"\"AVISO: la exportación se recortó a las {MaxRows:N0} {what} más recientes. " +
        "Usá los filtros de la pantalla para exportar por partes.\"";
}
