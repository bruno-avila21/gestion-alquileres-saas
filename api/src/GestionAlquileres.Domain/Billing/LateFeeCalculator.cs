namespace GestionAlquileres.Domain.Billing;

/// <summary>
/// Punitorio por mora, calculado día a día sobre el capital del cargo impago.
///
/// Función pura y sin dependencias a propósito: es la única regla de plata del módulo y tiene que
/// poder testearse sin base, sin reloj y sin el job que la usa.
/// </summary>
public static class LateFeeCalculator
{
    /// <summary>
    /// Días de mora de un cargo a la fecha dada: los transcurridos desde que venció el plazo de
    /// gracia. Devuelve 0 mientras el cargo no esté vencido o siga dentro de la tolerancia pactada.
    /// </summary>
    public static int DaysInArrears(DateOnly dueDate, int graceDays, DateOnly asOf)
    {
        var chargeableFrom = dueDate.AddDays(Math.Max(0, graceDays));
        var days = asOf.DayNumber - chargeableFrom.DayNumber;
        return days > 0 ? days : 0;
    }

    /// <summary>
    /// Punitorio devengado sobre <paramref name="principal"/>, redondeado a centavos.
    ///
    /// Se calcula SIEMPRE sobre el capital del cargo, nunca sobre punitorios ya devengados: el
    /// anatocismo sólo procede en los supuestos tasados del art. 770 CCyC y un SaaS no puede
    /// asumir que el contrato lo pactó.
    /// </summary>
    public static decimal Accrue(decimal principal, decimal dailyRatePercent, int daysInArrears)
    {
        if (principal <= 0m || dailyRatePercent <= 0m || daysInArrears <= 0) return 0m;

        var amount = principal * (dailyRatePercent / 100m) * daysInArrears;
        return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
    }
}
