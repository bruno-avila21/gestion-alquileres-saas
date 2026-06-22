namespace GestionAlquileres.Domain.Entities;

/// <summary>
/// Property owner (propietario) the agency manages on behalf of. Multi-tenant. A property may have
/// at most one owner; the agency's commission is configured per property.
/// </summary>
public class Owner : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }   // CUIT / DNI
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Cbu { get; set; }     // bank account for settlement payouts
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
