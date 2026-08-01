using GestionAlquileres.Application.Features.Contracts.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Contracts.Commands;

public class UpdateContractCommandHandler : IRequestHandler<UpdateContractCommand, ContractDto?>
{
    private readonly IContractRepository _repo;

    public UpdateContractCommandHandler(IContractRepository repo) => _repo = repo;

    public async Task<ContractDto?> Handle(UpdateContractCommand request, CancellationToken ct)
    {
        var contract = await _repo.GetByIdAsync(request.Id, ct);
        if (contract is null) return null;

        if (await _repo.HasActiveOverlapAsync(request.PropertyId, request.StartDate, request.EndDate, contract.Id, ct))
            throw new Common.Exceptions.BusinessException(
                "Ya existe un contrato activo para esta propiedad cuyo período se solapa con el indicado.");

        contract.PropertyId = request.PropertyId;
        contract.AppTenantId = request.AppTenantId;
        contract.StartDate = request.StartDate;
        contract.EndDate = request.EndDate;
        contract.MonthlyRent = request.MonthlyRent;
        contract.Currency = request.Currency;
        contract.AdjustmentType = request.AdjustmentType;
        contract.AdjustmentFrequency = request.AdjustmentFrequency;
        // El validador exige null cuando el tipo no es FixedPercent, así que cambiar de tipo
        // limpia el porcentaje en vez de dejarlo colgado.
        contract.AdjustmentPercent = request.AdjustmentPercent;
        contract.DayOfMonth = request.DayOfMonth;
        contract.DepositAmount = request.DepositAmount;
        contract.Notes = request.Notes?.Trim();

        await _repo.SaveChangesAsync(ct);

        var full = await _repo.GetByIdAsync(contract.Id, ct);
        return CreateContractCommandHandler.ToDto(full!);
    }
}
