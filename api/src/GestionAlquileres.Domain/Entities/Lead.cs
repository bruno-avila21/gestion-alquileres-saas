using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Domain.Entities;

/// <summary>
/// Consulta de un interesado (CRM de leads, bloque A3): llega desde el formulario público de una
/// publicación o de la sección de contacto del sitio, o se carga a mano desde el panel.
/// </summary>
public class Lead : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }

    /// <summary>Publicación sobre la que consultó. Null si fue una consulta general (sección "Contacto").</summary>
    public Guid? ListingId { get; set; }

    /// <summary>Se resuelve desde el listing al crear el lead; queda aunque el listing se borre después.</summary>
    public Guid? PropertyId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Message { get; set; } = string.Empty;

    public LeadSource Source { get; set; } = LeadSource.Manual;
    public LeadStatus Status { get; set; } = LeadStatus.New;

    /// <summary>Obligatorio cuando <see cref="Status"/> pasa a <see cref="LeadStatus.Lost"/>.</summary>
    public string? LostReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Último contacto: se actualiza al cambiar de estado o al agregar una nota.</summary>
    public DateTimeOffset? LastContactAt { get; set; }

    public Listing? Listing { get; set; }
    public Property? Property { get; set; }
    public ICollection<LeadNote> Notes { get; set; } = new List<LeadNote>();
}
