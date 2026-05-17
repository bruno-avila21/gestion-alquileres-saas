namespace GestionAlquileres.API.Contracts;

public record UpdateAppTenantRequest(
    string FirstName,
    string LastName,
    string Dni,
    string? Email,
    string? Phone,
    bool IsActive);
