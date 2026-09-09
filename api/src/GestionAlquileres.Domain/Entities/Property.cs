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

    /// <summary>Superficie total en m² (terreno + construido).</summary>
    public decimal? AreaM2 { get; set; }
    public string? Notes { get; set; }

    /// <summary>Owner this property belongs to (propietario). Null until assigned.</summary>
    public Guid? OwnerId { get; set; }

    /// <summary>Agency commission on collected rent, as a percentage (0–100). Null = no commission.</summary>
    public decimal? CommissionPct { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // ---- Ficha pública (lo que se muestra en el sitio de la inmobiliaria) ----

    /// <summary>Barrio o localidad dentro de la ciudad (ej. "Villa Pueyrredón", "Martínez").</summary>
    public string? Neighborhood { get; set; }

    /// <summary>Código de referencia que la inmobiliaria usa con el público (ej. "PAP8664371").</summary>
    public string? Code { get; set; }

    /// <summary>Descripción larga de la ficha, texto libre.</summary>
    public string? Description { get; set; }

    /// <summary>Ambientes (criterio argentino: living cuenta como uno).</summary>
    public int? Rooms { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? Garages { get; set; }

    /// <summary>Antigüedad en años; 0 = a estrenar.</summary>
    public int? AgeYears { get; set; }

    /// <summary>Superficie cubierta en m².</summary>
    public decimal? CoveredAreaM2 { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>Apto crédito hipotecario. Null = no especificado.</summary>
    public bool? SuitableForCredit { get; set; }

    /// <summary>
    /// Servicios y comodidades como etiquetas libres ("Gas natural", "Parrilla", "Apto mascotas").
    /// El catálogo sugerido vive en el frontend; el backend guarda lo que se cargó.
    /// </summary>
    public List<string> Features { get; set; } = new();
}
