namespace GestionAlquileres.Application.Features.Owners.DTOs;

/// <summary>
/// On-demand monthly settlement (rendición) for an owner: rent collected in the period, less the
/// agency commission, equals the net payable to the owner. Computed from settled payments; not persisted.
/// </summary>
public record OwnerSettlementDto(
    Guid OwnerId,
    string OwnerName,
    DateOnly PeriodFrom,
    DateOnly PeriodTo,
    decimal GrossCollected,
    decimal CommissionAmount,
    decimal NetToOwner,
    IReadOnlyList<OwnerSettlementLineDto> Lines);

public record OwnerSettlementLineDto(
    Guid PropertyId,
    string PropertyAddress,
    Guid ContractId,
    decimal Collected,
    decimal CommissionPct,
    decimal Commission,
    decimal Net);
