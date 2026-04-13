using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Domain.Entities;

/// <summary>
/// Global reference data from BCRA (ICL) or INDEC (IPC).
/// Explicitly NOT multi-tenant: no OrganizationId, no ITenantEntity, no HasQueryFilter in AppDbContext.
/// One row per (IndexType, Period) — unique constraint enforced at DB level.
/// Period is normalized to the first day of the month (e.g., 2024-03-01 = March 2024).
/// </summary>
public class IndexValue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public IndexType IndexType { get; set; }
    public DateOnly Period { get; set; }
    public decimal Value { get; set; }
    public decimal? VariationPct { get; set; }
    public string Source { get; set; } = string.Empty; // "BCRA" or "INDEC"
    public DateTimeOffset FetchedAt { get; set; } = DateTimeOffset.UtcNow;
}
