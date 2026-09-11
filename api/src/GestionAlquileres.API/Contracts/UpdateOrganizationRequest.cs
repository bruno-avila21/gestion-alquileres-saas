namespace GestionAlquileres.API.Contracts;

public record UpdateOrganizationRequest(
    string Name,
    string? LegalName,
    string? TaxId,
    string? Address,
    string? Phone,
    string? Email,
    string? BrandColor,
    /// <summary>Paleta del panel: uno de los valores de <c>PanelPalettes</c>. Null = la predeterminada.</summary>
    string? PanelPalette);
