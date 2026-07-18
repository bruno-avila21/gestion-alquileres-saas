using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Owners.DTOs;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Owners.Queries;

public class GetOwnerSettlementQueryHandler : IRequestHandler<GetOwnerSettlementQuery, OwnerSettlementDto>
{
    private readonly IOwnerRepository _ownerRepo;
    private readonly IPropertyRepository _propertyRepo;
    private readonly IContractRepository _contractRepo;
    private readonly ITransactionRepository _txRepo;

    public GetOwnerSettlementQueryHandler(
        IOwnerRepository ownerRepo,
        IPropertyRepository propertyRepo,
        IContractRepository contractRepo,
        ITransactionRepository txRepo)
    {
        _ownerRepo = ownerRepo;
        _propertyRepo = propertyRepo;
        _contractRepo = contractRepo;
        _txRepo = txRepo;
    }

    public async Task<OwnerSettlementDto> Handle(GetOwnerSettlementQuery request, CancellationToken ct)
    {
        var owner = await _ownerRepo.GetByIdAsync(request.OwnerId, ct)
            ?? throw new BusinessException("Propietario no encontrado.");

        if (request.PeriodTo < request.PeriodFrom)
            throw new BusinessException("El período final no puede ser anterior al inicial.");

        var lines = new List<OwnerSettlementLineDto>();
        var properties = await _propertyRepo.GetByOwnerAsync(owner.Id, ct);

        foreach (var property in properties)
        {
            var pct = property.CommissionPct ?? 0m;
            var contracts = await _contractRepo.ListAsync(null, property.Id, null, ct);

            foreach (var contract in contracts)
            {
                var txs = await _txRepo.GetByContractAsync(contract.Id, ct);
                // What's "collected" for the owner is money actually received — the Payment inflows in
                // the period (cash model). Both settlement paths now produce a Payment row: RegisterPayment
                // creates one directly, and mark-paid records a matching money-in Payment (audit A1), so
                // the owner's commission is no longer zeroed for charges settled via mark-paid.
                var collected = txs
                    .Where(t => t.Type == TransactionType.Payment
                                && t.Period >= request.PeriodFrom && t.Period <= request.PeriodTo)
                    .Sum(t => t.Amount);

                if (collected == 0m) continue;

                var commission = Math.Round(collected * pct / 100m, 2);
                lines.Add(new OwnerSettlementLineDto(
                    property.Id, property.Address, contract.Id, collected, pct, commission, collected - commission));
            }
        }

        var gross = lines.Sum(l => l.Collected);
        var commissionTotal = lines.Sum(l => l.Commission);

        return new OwnerSettlementDto(
            owner.Id, owner.Name, request.PeriodFrom, request.PeriodTo,
            gross, commissionTotal, gross - commissionTotal, lines);
    }
}
