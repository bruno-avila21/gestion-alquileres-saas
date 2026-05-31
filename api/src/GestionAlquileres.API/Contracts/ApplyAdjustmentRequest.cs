namespace GestionAlquileres.API.Contracts;

public record ApplyAdjustmentRequest(
    DateOnly? EffectiveDate,
    decimal? ManualNewRent,
    string? Notes
);
