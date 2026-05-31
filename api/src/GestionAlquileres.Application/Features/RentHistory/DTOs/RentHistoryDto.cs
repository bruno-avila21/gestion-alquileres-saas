using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Application.Features.RentHistory.DTOs;

public record RentHistoryDto(
    Guid Id,
    Guid ContractId,
    decimal PreviousRent,
    decimal NewRent,
    decimal AdjustmentFactor,
    AdjustmentType AdjustmentType,
    Guid? IndexValueId,
    DateOnly EffectiveDate,
    string? Notes,
    DateTimeOffset CreatedAt
);
