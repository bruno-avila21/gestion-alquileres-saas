namespace GestionAlquileres.Domain.Entities;

/// <summary>Nota de seguimiento agregada a un <see cref="Lead"/> desde el panel.</summary>
public class LeadNote : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid LeadId { get; set; }

    public string Text { get; set; } = string.Empty;

    public Guid CreatedByUserId { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Lead Lead { get; set; } = null!;
}
