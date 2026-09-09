namespace GestionAlquileres.API.Contracts;

public record UpdateOrganizationRequest(
    string Name,
    string? LegalName,
    string? TaxId,
    string? Address,
    string? Phone,
    string? Email,
    string? BrandColor);
