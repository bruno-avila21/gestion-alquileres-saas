namespace GestionAlquileres.Domain.Interfaces.Services;

/// <summary>One installment of an adjustment projection (mirrors indices-api's schedule item).</summary>
public record AdjustmentProjectionItem(
    int Number,
    DateOnly From,
    DateOnly To,
    decimal? Rent,
    decimal? Coefficient,
    decimal? VariationPct,
    bool IndexAvailable);

public record AdjustmentProjection(
    decimal CurrentRent,
    IReadOnlyList<AdjustmentProjectionItem> Schedule,
    string? Notes);

/// <summary>Computes a rent-adjustment projection via the standalone indices-api.</summary>
public interface IIndicesCalculator
{
    Task<AdjustmentProjection?> CalculateAsync(
        string index,
        decimal initialRent,
        DateOnly startDate,
        int frequencyMonths,
        DateOnly? until,
        CancellationToken ct = default);
}
