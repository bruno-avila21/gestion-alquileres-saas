using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Domain.Entities;

public class Property : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public PropertyType PropertyType { get; set; }
    public decimal? AreaM2 { get; set; }
    public string? Notes { get; set; }

    /// <summary>Owner this property belongs to (propietario). Null until assigned.</summary>
    public Guid? OwnerId { get; set; }

    /// <summary>Agency commission on collected rent, as a percentage (0–100). Null = no commission.</summary>
    public decimal? CommissionPct { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
