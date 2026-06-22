namespace GestionAlquileres.API.Contracts;

public record CreateOwnerRequest(
    string Name,
    string? TaxId,
    string? Email,
    string? Phone,
    string? Cbu,
    string? Notes);
