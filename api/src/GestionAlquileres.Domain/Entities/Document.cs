namespace GestionAlquileres.Domain.Entities;

public class Document : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid ContractId { get; set; }
    public string FileName { get; set; } = "";
    public string StorageKey { get; set; } = "";
    public string MimeType { get; set; } = "";
    public long SizeBytes { get; set; }
    public Guid UploadedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Whether the tenant may see/download this document (audit A2). Secure by default: documents are
    /// private to staff until explicitly shared, so an admin uploading an internal note never leaks it
    /// to the tenant. The tenant portal filters on this; staff see everything within the organization.
    /// </summary>
    public bool IsVisibleToTenant { get; set; }
}
