using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Application.Common.Billing;

/// <summary>
/// Alta y actualización del punitorio de un cargo impago.
///
/// Vive fuera del job porque tiene DOS llamadores con exigencias distintas: el devengamiento diario
/// (que devenga hasta hoy) y los dos caminos de cobro (que devengan hasta la fecha del pago, para
/// congelar el punitorio en el día correcto). Duplicar la regla en ambos lados garantizaba que se
/// separaran.
/// </summary>
public interface ILateFeeAccrualService
{
    /// <summary>
    /// Devenga el punitorio de <paramref name="charge"/> hasta <paramref name="asOf"/> inclusive,
    /// creando la transacción de punitorio si es la primera vez o ajustando su importe si ya existe.
    ///
    /// No llama a SaveChanges: el llamador decide la unidad de trabajo (el cobro necesita que el
    /// devengamiento y la imputación entren en la misma).
    /// </summary>
    /// <returns>El punitorio resultante, o null si el cargo no devenga nada todavía.</returns>
    Task<Transaction?> AccrueAsync(
        Transaction charge, decimal dailyRate, int graceDays, DateOnly asOf, CancellationToken ct);
}
