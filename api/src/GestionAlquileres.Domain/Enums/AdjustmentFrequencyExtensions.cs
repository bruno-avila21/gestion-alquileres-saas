namespace GestionAlquileres.Domain.Enums;

public static class AdjustmentFrequencyExtensions
{
    /// <summary>
    /// Cantidad de meses de un período de ajuste.
    ///
    /// Única fuente de verdad: este mapeo estaba duplicado en el handler del ajuste, en la
    /// proyección y en el job mensual, y ya había divergido entre ellos. Con dos frecuencias nuevas
    /// la duplicación pasaba a ser insostenible.
    ///
    /// Lanza ante un valor desconocido en vez de caer a un default silencioso: el `_ => 3` anterior
    /// convertía un enum inválido en un ajuste trimestral que nadie pidió.
    /// </summary>
    public static int ToMonths(this AdjustmentFrequency frequency) => frequency switch
    {
        AdjustmentFrequency.Monthly => 1,
        AdjustmentFrequency.Quarterly => 3,
        AdjustmentFrequency.FourMonthly => 4,
        AdjustmentFrequency.SemiAnnual => 6,
        AdjustmentFrequency.Annual => 12,
        _ => throw new ArgumentOutOfRangeException(
            nameof(frequency), frequency, "Frecuencia de ajuste no soportada."),
    };
}
