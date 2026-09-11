using FluentValidation;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Application.Features.Contracts.Commands;

/// <summary>
/// Reglas compartidas entre el alta y la edición de contratos. Estaban duplicadas y ya habían
/// empezado a divergir; con el porcentaje de ajuste sumado, mantenerlas en dos lugares garantizaba
/// que una de las dos se quedara atrás.
/// </summary>
internal static class ContractRules
{
    /// <summary>Tope del porcentaje: coincide con la precisión (6,3) de la columna.</summary>
    private const decimal MaxPercent = 999.999m;

    /// <summary>Tope de la tasa punitoria diaria: coincide con la precisión (6,4) de la columna.</summary>
    private const decimal MaxDailyRate = 99.9999m;

    /// <summary>Tope de la tolerancia. Más de tres meses de gracia no es una tolerancia: es no tener punitorio.</summary>
    private const int MaxGraceDays = 90;

    public static void Apply<T>(
        AbstractValidator<T> v,
        Func<T, AdjustmentType> type,
        Func<T, decimal?> percent,
        Func<T, decimal?> lateFeeDailyRate,
        Func<T, int> lateFeeGraceDays)
    {
        // Sin IsInEnum, un adjustmentType inválido se persistía como entero crudo y el motor de
        // ajustes lo trataba como IPC: el contrato terminaba ajustado con el índice equivocado, y
        // ese importe quedaba como base del ajuste siguiente.
        v.RuleFor(x => type(x)).IsInEnum()
            .WithName("adjustmentType")
            .WithMessage("Tipo de ajuste no soportado.");

        // Ojo: en FluentValidation un WithMessage al final de una cadena aplica sólo a la ÚLTIMA
        // regla. Encadenar NotNull().GreaterThan(0).WithMessage(...) dejaba el caso "sin
        // porcentaje" con el mensaje por defecto en inglés, así que cada regla lleva el suyo.
        v.RuleFor(x => percent(x))
            .NotNull()
            .WithName("adjustmentPercent")
            .WithMessage("Un contrato con ajuste por porcentaje fijo requiere el porcentaje pactado.")
            .When(x => type(x) == AdjustmentType.FixedPercent);

        v.RuleFor(x => percent(x))
            .GreaterThan(0)
            .WithName("adjustmentPercent")
            .WithMessage("El porcentaje de ajuste debe ser mayor a 0.")
            .When(x => type(x) == AdjustmentType.FixedPercent && percent(x).HasValue);

        v.RuleFor(x => percent(x))
            .LessThanOrEqualTo(MaxPercent)
            .WithName("adjustmentPercent")
            .WithMessage($"El porcentaje de ajuste no puede superar {MaxPercent}.")
            .When(x => percent(x).HasValue);

        v.RuleFor(x => percent(x))
            .Null()
            .WithName("adjustmentPercent")
            .WithMessage("El porcentaje de ajuste sólo aplica a contratos de porcentaje fijo.")
            .When(x => type(x) != AdjustmentType.FixedPercent);

        // Punitorio. Es opcional: null o 0 significa que el contrato no lo pactó. Lo que no se
        // admite es una tasa negativa, que convertiría el punitorio en un descuento por pagar tarde.
        v.RuleFor(x => lateFeeDailyRate(x))
            .GreaterThanOrEqualTo(0)
            .WithName("lateFeeDailyRate")
            .WithMessage("La tasa punitoria no puede ser negativa.")
            .When(x => lateFeeDailyRate(x).HasValue);

        v.RuleFor(x => lateFeeDailyRate(x))
            .LessThanOrEqualTo(MaxDailyRate)
            .WithName("lateFeeDailyRate")
            .WithMessage($"La tasa punitoria diaria no puede superar {MaxDailyRate}%.")
            .When(x => lateFeeDailyRate(x).HasValue);

        v.RuleFor(x => lateFeeGraceDays(x))
            .InclusiveBetween(0, MaxGraceDays)
            .WithName("lateFeeGraceDays")
            .WithMessage($"Los días de gracia deben estar entre 0 y {MaxGraceDays}.");
    }
}
