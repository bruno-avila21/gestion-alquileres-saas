using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Domain.Entities;

public class RentHistory : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid ContractId { get; set; }
    public decimal PreviousRent { get; set; }
    public decimal NewRent { get; set; }
    public decimal AdjustmentFactor { get; set; }
    public AdjustmentType AdjustmentType { get; set; }
    public Guid? IndexValueId { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
