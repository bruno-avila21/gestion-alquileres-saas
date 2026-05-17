namespace GestionAlquileres.API.Contracts;

public record CreateAppTenantRequest(
    string FirstName,
    string LastName,
    string Dni,
    string? Email,
    string? Phone);
