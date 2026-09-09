namespace GestionAlquileres.Domain.Entities;

/// <summary>
/// Foto de la ficha pública. A diferencia de <see cref="Document"/> es contenido público: se sirve
/// por un endpoint anónimo del sitio de la inmobiliaria, nunca por URL directa al storage.
/// </summary>
public class PropertyPhoto : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int SortOrder { get; set; }
    public bool IsCover { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
