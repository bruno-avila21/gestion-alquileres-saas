namespace GestionAlquileres.Application.Common.DTOs;

public record AuthResponseDto(
    string Token,
    Guid UserId,
    string Email,
    string Role,
    Guid OrganizationId,
    string OrganizationSlug);
