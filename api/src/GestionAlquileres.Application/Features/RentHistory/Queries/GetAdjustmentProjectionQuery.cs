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

        var index = contract.AdjustmentType == AdjustmentType.ICL ? "ICL" : "IPC";
        var frequencyMonths = contract.AdjustmentFrequency switch
        {
            AdjustmentFrequency.Monthly => 1,
            AdjustmentFrequency.Quarterly => 3,
            AdjustmentFrequency.Annual => 12,
            _ => 3,
        };

        return await _calculator.CalculateAsync(
            index, initialRent, contract.StartDate, frequencyMonths, until: null, ct);
    }
}
