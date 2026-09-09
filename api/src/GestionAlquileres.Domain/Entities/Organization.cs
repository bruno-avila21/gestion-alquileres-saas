namespace GestionAlquileres.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Plan { get; set; } = "free";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // ---- Marca de la inmobiliaria (bloque PDF recibos/liquidaciones) ----

    /// <summary>Razón social, si difiere del nombre comercial.</summary>
    public string? LegalName { get; set; }

    /// <summary>CUIT. Se guarda tal cual lo escriben.</summary>
    public string? TaxId { get; set; }

    /// <summary>Domicilio de la inmobiliaria.</summary>
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    /// <summary>Clave en el storage (S3/MinIO) del logo. Null = sin logo cargado.</summary>
    public string? LogoStorageKey { get; set; }

    /// <summary>Hex `#RRGGBB` para encabezado y totales del PDF. Null = gris neutro.</summary>
    public string? BrandColor { get; set; }

    /// <summary>
    /// Contador de recibos de la organización. Arranca en 0; se incrementa de forma atómica la
    /// primera vez que se pide el recibo de cada transacción de pago (ver IOrganizationRepository).
    /// </summary>
    public long ReceiptSequence { get; set; }
}
