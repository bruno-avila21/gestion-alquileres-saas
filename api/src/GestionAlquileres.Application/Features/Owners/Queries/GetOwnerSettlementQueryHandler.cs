using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Owners.DTOs;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Owners.Queries;

public class GetOwnerSettlementQueryHandler : IRequestHandler<GetOwnerSettlementQuery, OwnerSettlementDto>
{
    private readonly IOwnerRepository _ownerRepo;
    private readonly ITransactionRepository _txRepo;

    // Ya no hacen falta los repositorios de propiedades ni de contratos: la consulta agregada
    // resuelve el join completo en la base.
    public GetOwnerSettlementQueryHandler(IOwnerRepository ownerRepo, ITransactionRepository txRepo)
    {
        _ownerRepo = ownerRepo;
        _txRepo = txRepo;
    }

    public async Task<OwnerSettlementDto> Handle(GetOwnerSettlementQuery request, CancellationToken ct)
    {
        var owner = await _ownerRepo.GetByIdAsync(request.OwnerId, ct)
            ?? throw new BusinessException("Propietario no encontrado.");

        if (request.PeriodTo < request.PeriodFrom)
            throw new BusinessException("El período final no puede ser anterior al inicial.");

        // Una sola consulta agregada. Antes se recorría propiedad por propiedad y contrato por
        // contrato, trayendo TODAS las transacciones de cada uno para filtrar el período en memoria.
        var collectedRows = await _txRepo.GetCollectedByOwnerAsync(
            owner.Id, request.PeriodFrom, request.PeriodTo, ct);

        var lines = new List<OwnerSettlementLineDto>();
        foreach (var row in collectedRows)
        {
            // Un contrato cuyos cobros del período suman exactamente cero no aporta nada a la
            // liquidación y ensuciaría el detalle.
            if (row.Collected == 0m) continue;

            var pct = row.CommissionPct ?? 0m;
            var commission = Math.Round(row.Collected * pct / 100m, 2);
            lines.Add(new OwnerSettlementLineDto(
                row.PropertyId, row.PropertyAddress, row.ContractId,
                row.Collected, pct, commission, row.Collected - commission));
        }

        var gross = lines.Sum(l => l.Collected);
        var commissionTotal = lines.Sum(l => l.Commission);

        return new OwnerSettlementDto(
            owner.Id, owner.Name, request.PeriodFrom, request.PeriodTo,
            gross, commissionTotal, gross - commissionTotal, lines);
    }
}
