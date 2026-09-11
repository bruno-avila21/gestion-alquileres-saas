using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.RentHistory.Queries;

/// <summary>
/// Projects how a contract's rent evolves under its index, via the standalone indices-api.
/// Returns null for Manual contracts (no index-based projection).
/// </summary>
public record GetAdjustmentProjectionQuery(Guid ContractId) : IRequest<AdjustmentProjection?>;

public class GetAdjustmentProjectionQueryHandler : IRequestHandler<GetAdjustmentProjectionQuery, AdjustmentProjection?>
{
    private readonly IContractRepository _contracts;
    private readonly IRentHistoryRepository _history;
    private readonly IIndicesCalculator _calculator;

    public GetAdjustmentProjectionQueryHandler(
        IContractRepository contracts, IRentHistoryRepository history, IIndicesCalculator calculator)
    {
        _contracts = contracts;
        _history = history;
        _calculator = calculator;
    }

    public async Task<AdjustmentProjection?> Handle(GetAdjustmentProjectionQuery request, CancellationToken ct)
    {
        var contract = await _contracts.GetByIdAsync(request.ContractId, ct)
            ?? throw new BusinessException("Contrato no encontrado.");

        // Manual contracts are not index-driven — nothing to project.
        if (contract.AdjustmentType == AdjustmentType.Manual)
            return null;

        // Project from the original rent at contract start. MonthlyRent is the *current* (already
        // adjusted) value, so use the oldest history record's PreviousRent when adjustments exist.
        var history = await _history.GetByContractAsync(contract.Id, ct);
        var initialRent = history.Count > 0 ? history[^1].PreviousRent : contract.MonthlyRent;

        var frequencyMonths = contract.AdjustmentFrequency.ToMonths();

        // Porcentaje fijo: el escalonado es determinístico, así que se proyecta acá mismo sin
        // depender de indices-api. Es además el único tipo cuya proyección no puede fallar por
        // indisponibilidad de un índice.
        if (contract.AdjustmentType == AdjustmentType.FixedPercent)
            return ProjectFixedPercent(contract, initialRent, frequencyMonths);

        var index = contract.AdjustmentType == AdjustmentType.ICL ? "ICL" : "IPC";

        return await _calculator.CalculateAsync(
            index, initialRent, contract.StartDate, frequencyMonths, until: null, ct);
    }

    /// <summary>
    /// Escalonado por porcentaje fijo, calculado localmente: alquiler × (1 + p)^k para cada período
    /// entre el inicio y el fin del contrato.
    /// </summary>
    private static AdjustmentProjection ProjectFixedPercent(
        Domain.Entities.Contract contract, decimal initialRent, int frequencyMonths)
    {
        var percent = contract.AdjustmentPercent ?? 0m;
        var items = new List<AdjustmentProjectionItem>();

        var rent = initialRent;
        var from = contract.StartDate;
        var number = 1;

        while (from < contract.EndDate)
        {
            var to = from.AddMonths(frequencyMonths).AddDays(-1);
            if (to > contract.EndDate) to = contract.EndDate;

            // El primer período corre al alquiler original; recién a partir del segundo se aplica
            // el escalón, igual que hace el motor de ajustes.
            if (number > 1)
                rent = Math.Round(rent * (100m + percent) / 100m, 2);

            items.Add(new AdjustmentProjectionItem(
                Number: number,
                From: from,
                To: to,
                Rent: rent,
                Coefficient: number > 1 ? Math.Round((100m + percent) / 100m, 6) : 1m,
                VariationPct: number > 1 ? percent : 0m,
                IndexAvailable: true));

            from = from.AddMonths(frequencyMonths);
            number++;

            // Cinturón de seguridad ante un contrato con fechas absurdas.
            if (number > 240) break;
        }

        return new AdjustmentProjection(
            CurrentRent: contract.MonthlyRent,
            Schedule: items,
            Notes: $"Escalonado por porcentaje fijo pactado: {percent}% cada {frequencyMonths} meses.");
    }
}
