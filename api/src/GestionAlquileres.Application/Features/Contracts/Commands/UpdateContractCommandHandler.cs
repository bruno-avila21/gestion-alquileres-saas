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

        contract.PropertyId = request.PropertyId;
        contract.AppTenantId = request.AppTenantId;
        contract.StartDate = request.StartDate;
        contract.EndDate = request.EndDate;
        contract.MonthlyRent = request.MonthlyRent;
        contract.Currency = request.Currency;
        contract.AdjustmentType = request.AdjustmentType;
        contract.AdjustmentFrequency = request.AdjustmentFrequency;
        contract.DayOfMonth = request.DayOfMonth;
        contract.DepositAmount = request.DepositAmount;
        contract.Notes = request.Notes?.Trim();

        await _repo.SaveChangesAsync(ct);

        var full = await _repo.GetByIdAsync(contract.Id, ct);
        return CreateContractCommandHandler.ToDto(full!);
    }
}
