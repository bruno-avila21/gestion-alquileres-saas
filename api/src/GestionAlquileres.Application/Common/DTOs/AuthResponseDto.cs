namespace GestionAlquileres.Application.Common.DTOs;

public record AuthResponseDto(
    string Token,
    Guid UserId,
    string Email,
    string Role,
    Guid OrganizationId,
    string OrganizationSlug,
    /// <summary>Cuando es true el cliente debe mandar al usuario a cambiar la contraseña antes de operar.</summary>
    bool MustChangePassword = false);
