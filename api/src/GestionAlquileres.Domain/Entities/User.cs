using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Domain.Entities;

public class User : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Obliga a cambiar la contraseña antes de poder operar. Se activa cuando la credencial la
    /// generó el sistema y viajó por un canal fuera de banda (la invitación al inquilino, que el
    /// administrador le pasa por WhatsApp o email): esa contraseña queda en el historial de esa
    /// conversación, así que no puede ser la credencial definitiva.
    /// </summary>
    public bool MustChangePassword { get; set; }

    /// <summary>Última vez que la contraseña cambió. Null si nunca se cambió desde el alta.</summary>
    public DateTimeOffset? PasswordChangedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
