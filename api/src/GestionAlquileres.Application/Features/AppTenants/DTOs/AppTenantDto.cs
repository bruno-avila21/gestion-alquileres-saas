namespace GestionAlquileres.Application.Features.AppTenants.DTOs;

public record AppTenantDto(
    Guid Id,
    Guid OrganizationId,
    string FirstName,
    string LastName,
    string Dni,
    string? Email,
    string? Phone,
    Guid? UserId,
    bool IsActive,
    DateTimeOffset CreatedAt);
