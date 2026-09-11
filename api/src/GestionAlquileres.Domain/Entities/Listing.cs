using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Domain.Entities;

/// <summary>
/// Publicación de una propiedad: la oferta comercial (venta/alquiler, precio, estado) que ve el público.
/// Una propiedad puede tener más de una publicación a lo largo del tiempo (se vendió, después se alquila),
/// pero a lo sumo una publicada por tipo de operación.
/// </summary>
public class Listing : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }

    public OperationType OperationType { get; set; }
    public decimal Price { get; set; }
    public Currency Currency { get; set; } = Currency.ARS;

    /// <summary>Expensas mensuales en ARS, si aplica.</summary>
    public decimal? Expenses { get; set; }

    public ListingStatus Status { get; set; } = ListingStatus.Draft;

    /// <summary>Título comercial de la ficha ("Departamento dos ambientes en Villa Pueyrredón").</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Destacada: sube primero en el listado público.</summary>
    public bool IsFeatured { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Property Property { get; set; } = null!;
}
