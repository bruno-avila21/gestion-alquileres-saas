using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Owners.DTOs;
using GestionAlquileres.Domain.Entities;
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

        ValidatePeriod(request.PeriodFrom, request.PeriodTo);

        var collectedRows = await _txRepo.GetCollectedByOwnerAsync(
            owner.Id, request.PeriodFrom, request.PeriodTo, ct);

        return BuildDto(owner, request.PeriodFrom, request.PeriodTo, collectedRows);
    }

    /// <summary>El GET a 409 histórico de este endpoint. La variante PDF (GetOwnerSettlementPdfQueryHandler)
    /// hace su propio chequeo de existencia -> 404 y sólo reusa esta validación de rango.</summary>
    public static void ValidatePeriod(DateOnly from, DateOnly to)
    {
        if (to < from)
            throw new BusinessException("El período final no puede ser anterior al inicial.");
    }

    /// <summary>
    /// Construye el DTO a partir de las filas ya agregadas por ITransactionRepository.GetCollectedByOwnerAsync.
    /// Compartido con el handler del PDF (Parte C del bloque de recibos/liquidaciones): "el PDF sólo
    /// formatea" lo que esta consulta ya calcula.
    /// </summary>
    public static OwnerSettlementDto BuildDto(
        Owner owner, DateOnly from, DateOnly to, IReadOnlyList<OwnerCollectedRow> collectedRows)
    {
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
            owner.Id, owner.Name, from, to, gross, commissionTotal, gross - commissionTotal, lines);
    }
}
