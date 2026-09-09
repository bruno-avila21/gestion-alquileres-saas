using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Domain.Entities;

public class Contract : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid AppTenantId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public Currency Currency { get; set; } = Currency.ARS;
    public AdjustmentType AdjustmentType { get; set; }
    public AdjustmentFrequency AdjustmentFrequency { get; set; }

    /// <summary>
    /// Porcentaje pactado que se aplica en cada período cuando <see cref="AdjustmentType"/> es
    /// <see cref="AdjustmentType.FixedPercent"/> (por ejemplo 8 para un 8% trimestral).
    /// Null para el resto de los tipos, que no lo usan.
    /// </summary>
    public decimal? AdjustmentPercent { get; set; }
    public int DayOfMonth { get; set; } = 1;
    public decimal? DepositAmount { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Active;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? TerminatedAt { get; set; }

    public Property Property { get; set; } = null!;
    public AppTenant AppTenant { get; set; } = null!;
}
