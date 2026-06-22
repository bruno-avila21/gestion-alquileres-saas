namespace GestionAlquileres.Application.Features.Owners.DTOs;

public record OwnerDto(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string? TaxId,
    string? Email,
    string? Phone,
    string? Cbu,
    string? Notes,
    bool IsActive,
    DateTimeOffset CreatedAt);
