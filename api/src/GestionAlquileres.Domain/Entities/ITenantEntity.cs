namespace GestionAlquileres.Domain.Entities;

/// <summary>
/// Marker interface for entities scoped to an Organization (tenant).
/// AppDbContext automatically applies OrganizationId HasQueryFilter to these.
/// Organization itself does NOT implement this — it IS the tenant root.
/// </summary>
public interface ITenantEntity
{
    Guid OrganizationId { get; }
}
