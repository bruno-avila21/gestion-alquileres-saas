namespace GestionAlquileres.API.Contracts;

public record UpdateOwnerRequest(
    string Name,
    string? TaxId,
    string? Email,
    string? Phone,
    string? Cbu,
    string? Notes,
    bool IsActive);
