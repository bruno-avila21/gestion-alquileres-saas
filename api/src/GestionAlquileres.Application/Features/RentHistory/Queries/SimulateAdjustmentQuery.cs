using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.RentHistory.Queries;

/// <summary>
/// Simula un ajuste sobre parámetros sueltos, sin contrato y sin escribir nada.
///
/// Es distinta de <see cref="GetAdjustmentProjectionQuery"/>, que proyecta un contrato que ya
/// existe: ésta contesta la pregunta previa a la firma —"si pacto ICL trimestral sobre
/// $ 500.000 desde marzo, ¿en cuánto termina el año que viene?"— que hoy la inmobiliaria
/// resuelve en una planilla aparte. Por eso no toca la base: no hay nada que persistir en un
/// contrato que todavía no se firmó.
/// </summary>
public record SimulateAdjustmentQuery(
    AdjustmentType Type,
    decimal InitialRent,
    DateOnly StartDate,
    AdjustmentFrequency Frequency,
    /// <summary>Sólo para <see cref="AdjustmentType.FixedPercent"/>: el porcentaje pactado por período.</summary>
    decimal? Percent,
    DateOnly? Until) : IRequest<AdjustmentProjection?>;

public class SimulateAdjustmentQueryHandler : IRequestHandler<SimulateAdjustmentQuery, AdjustmentProjection?>
{
    /// <summary>Cuántos períodos proyectar cuando no se pide un tope: cubre un contrato típico de 3 años.</summary>
    private const int DefaultPeriods = 12;

    private readonly IIndicesCalculator _calculator;

    public SimulateAdjustmentQueryHandler(IIndicesCalculator calculator) => _calculator = calculator;

    public async Task<AdjustmentProjection?> Handle(SimulateAdjustmentQuery request, CancellationToken ct)
    {
        if (request.InitialRent <= 0)
            throw new BusinessException("El alquiler inicial debe ser mayor a 0.");

        // Manual no tiene fórmula: el importe lo fija el operador período a período, así que no
        // hay nada que simular. Se devuelve null y el API responde 204, igual que la proyección
        // de un contrato manual.
        if (request.Type == AdjustmentType.Manual) return null;

        var months = request.Frequency.ToMonths();

        if (request.Type == AdjustmentType.FixedPercent)
        {
            if (request.Percent is not > 0)
                throw new BusinessException("Un ajuste por porcentaje fijo requiere el porcentaje pactado.");
            return ProjectFixedPercent(request, months);
        }

        var index = request.Type == AdjustmentType.ICL ? "ICL" : "IPC";
        return await _calculator.CalculateAsync(
            index, request.InitialRent, request.StartDate, months, request.Until, ct);
    }

    /// <summary>
    /// El escalonado por porcentaje es determinístico y no depende de ningún índice externo,
    /// así que se resuelve acá y es el único tipo cuya simulación no puede fallar porque el
    /// BCRA o el INDEC no respondan.
    /// </summary>
    private static AdjustmentProjection ProjectFixedPercent(SimulateAdjustmentQuery request, int months)
    {
        var factor = 1m + (request.Percent!.Value / 100m);
        var items = new List<AdjustmentProjectionItem>();

        var rent = request.InitialRent;
        var from = request.StartDate;

        for (var n = 1; n <= DefaultPeriods; n++)
        {
            var to = from.AddMonths(months).AddDays(-1);
            if (request.Until is not null && from > request.Until) break;

            items.Add(new AdjustmentProjectionItem(
                Number: n,
                From: from,
                To: to,
                Rent: Math.Round(rent, 2),
                Coefficient: Math.Round(rent / request.InitialRent, 4),
                VariationPct: n == 1 ? 0m : Math.Round(request.Percent!.Value, 2),
                IndexAvailable: true));

            rent *= factor;
            from = to.AddDays(1);
        }

        return new AdjustmentProjection(
            CurrentRent: request.InitialRent,
            Schedule: items,
            Notes: $"Porcentaje fijo del {request.Percent}% por período. No depende de ningún índice.");
    }
}
